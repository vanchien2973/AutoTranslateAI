# AutoTranslateAI

> Hệ thống tự động lồng tiếng đa ngôn ngữ cho video (YouTube, TikTok, Douyin...) và xuất bản tối ưu SEO.

AutoTranslateAI tải video nguồn, tách nhạc nền khỏi giọng nói, chuyển giọng nói thành văn bản, dịch sang ngôn ngữ đích, cho phép **người dùng chỉnh sửa bản dịch trước khi render**, tạo giọng đọc TTS, ghép lại audio + nhạc nền + logo, **tùy chọn gắn phụ đề (có/không, hardsub/softsub)**, và (tùy chọn) tự động đăng tải lên YouTube/Facebook.

**Ngôn ngữ giọng đọc và ngôn ngữ phụ đề tách rời, chọn độc lập** — ví dụ nói tiếng Anh + phụ đề tiếng Việt (luyện nghe), hoặc nói tiếng Việt + phụ đề tiếng Anh, hoặc nói + phụ đề cùng tiếng Anh (luyện nghe-đọc). Rất hợp cho cả mục đích lồng tiếng lẫn học ngôn ngữ.

> ### ⚙️ Phạm vi triển khai: **chỉ deploy local bằng Docker**
> Dự án này chỉ chạy local qua `docker-compose`, **không deploy lên production/cloud**. Hệ quả:
> - **Bỏ qua** các phần production-only: managed services (Azure Database, managed RabbitMQ...), horizontal scaling nhiều instance, load balancer, CI/CD deploy, monitoring/alerting hạ tầng, Redis backplane cho SignalR (1 instance không cần).
> - Toàn bộ stack chạy trong Docker: API + Worker + PostgreSQL + RabbitMQ — đều là container local.
> - **File storage:** dùng **Cloudflare R2** (S3-compatible) cho video output — chọn R2 vì **không tính phí egress** (rất hợp video, tránh phí tải ra đắt đỏ của Azure/AWS), có free tier 10 GB/tháng. File intermediate (audio.wav, vocals.wav...) để trên **ổ đĩa local qua volume mount**, không cần đẩy lên cloud. Interface storage giữ nguyên, implementation dùng AWS SDK trỏ tới R2 endpoint.
> - **Stack chốt (tối ưu chi phí):** STT = `whisper.net` local · LLM dịch = **GPT-4.1 Nano + Batch API** · TTS = **Azure Speech free tier (500K ký tự/tháng)** · Storage = **R2**. Xem chi tiết + ước tính ở mục [Chi phí sử dụng](#chi-phí-sử-dụng).
> - Muốn offline/$0 tuyệt đối: thay LLM bằng Ollama, TTS bằng Piper/Coqui — đánh đổi chất lượng.
> - Auto-publish (Phase 6) vẫn gọi YouTube/Facebook API nếu dùng — đây là tính năng tùy chọn, không bắt buộc cho bản local.

---

## Mục lục

- [Tổng quan kiến trúc](#tổng-quan-kiến-trúc)
- [Pipeline xử lý](#pipeline-xử-lý)
- [Technical stack](#technical-stack)
- [Cấu trúc thư mục](#cấu-trúc-thư-mục)
- [Database design](#database-design)
- [Worker architecture](#worker-architecture)
- [API endpoints](#api-endpoints)
- [Testing & TDD](#testing--tdd)
- [Chi phí sử dụng](#chi-phí-sử-dụng)
- [Điểm kỹ thuật cần lưu ý](#điểm-kỹ-thuật-cần-lưu-ý)
- [Yêu cầu môi trường](#yêu-cầu-môi-trường)

---

## Tổng quan kiến trúc

Dự án theo **Clean Architecture** với phần xử lý nặng tách thành **Worker riêng** giao tiếp qua message queue. Vì xử lý video tốn thời gian (vài phút/video) và có **điểm dừng chờ người dùng review**, hệ thống không xử lý trực tiếp trong HTTP request.

Đặc điểm quan trọng nhất về kiến trúc: pipeline có **human-in-the-loop** — chạy tự động đến bước dịch, **dừng lại chờ user sửa transcript/translation**, rồi mới tiếp tục render.

```
┌─────────┐   enqueue   ┌──────────────┐
│   API   │ ──────────► │   RabbitMQ   │
│ (.NET)  │             │   (queue)    │
└────┬────┘             └──────┬───────┘
     │ SignalR                 │ consume
     │ (progress)         ┌────▼─────────┐
     ▼                    │   Workers    │
┌─────────┐               │ Phase1/Phase2│
│ Client  │               └────┬─────────┘
└─────────┘                    │ gọi CLI tools
                          ┌────▼──────────────────────┐
                          │ yt-dlp · ffmpeg · demucs   │
                          │ whisper.net · Azure TTS · GPT-4.1 Nano │
                          └────────────────────────────┘
```

---

## Pipeline xử lý

Pipeline chia làm **2 phase**, ngăn bởi điểm chờ người dùng:

```
PHASE 1 (auto):  Download → ExtractAudio → SeparateBgm → Transcribe → Translate
                 │
                 ▼
            [PAUSE — chờ user review/sửa transcript + translation]
                 │
                 ▼
PHASE 2 (auto):  TTS → [GenSubtitle] → Mix → Render → Publish
```

> `GenSubtitle` chỉ chạy khi user chọn có phụ đề. `Render` áp dụng hardsub / softsub / không phụ đề tùy cấu hình.

### Chi tiết từng bước

| Bước | Mô tả | Công cụ |
|------|-------|---------|
| **Download** | Tải video từ URL hoặc đọc file local | `yt-dlp` |
| **ExtractAudio** | Tách audio khỏi video | `ffmpeg` |
| **SeparateBgm** | Tách giọng nói / nhạc nền (BGM) | `demucs` (Demucs AI) |
| **Transcribe** | Speech-to-text + timestamp theo segment | `whisper.net` (local) |
| **Translate** | Dịch segment cho ngôn ngữ audio và/hoặc phụ đề (1 hoặc 2 lần tùy combo, giữ timestamp) | **GPT-4.1 Nano** (OpenAI, qua Batch API) |
| **⏸ Review** | User sửa transcript/bản dịch, gán giọng nhân vật | (UI) |
| **TTS** | (Nếu lồng tiếng) tạo giọng đọc theo **ngôn ngữ audio** (Nam/Nữ) | **Azure Speech** (free tier 500K ký tự/tháng) |
| **GenSubtitle** | (Tùy chọn) sinh file phụ đề `.srt`/`.vtt` theo **ngôn ngữ phụ đề** | nội bộ |
| **Mix** | Ghép giọng đọc + nhạc nền (ducking -12dB) | `ffmpeg` |
| **Render** | Render video cuối + logo watermark; phụ đề tùy chọn (hardsub / softsub / không) | `ffmpeg` |
| **Publish** | (Tùy chọn) Đăng lên YouTube / Facebook | YouTube Data API v3 / Graph API |

### Cấu hình hỗ trợ (theo UI)

- **Nguồn video:** URL (YouTube/TikTok/Douyin) hoặc đường dẫn file local
- **Ngôn ngữ gốc:** English, Chinese... (hoặc tự động nhận diện)
- **Ngôn ngữ giọng đọc (audio):** chọn được nhiều ngôn ngữ (Việt, Anh, Trung, Nhật, Hàn...) — quyết định bản dịch dùng cho TTS + bộ giọng
- **Ngôn ngữ phụ đề:** chọn **độc lập** với ngôn ngữ giọng đọc — có thể trùng hoặc khác
- **Lồng tiếng (dubbing):** bật/tắt — nếu tắt thì giữ nguyên audio gốc, chỉ thêm phụ đề
- **Giọng đọc lồng tiếng:** Nữ (Female) / Nam (Male) — danh sách giọng lọc theo ngôn ngữ audio
- **Xử lý nhạc nền (BGM):** Tách AI (Demucs) / Nền nhẹ (Duck -12dB) / Không nhạc nền
- **Ducking BGM Volume:** mặc định -12 dB
- **Gán giọng nhân vật thủ công:** dừng lại để chọn giọng cho từng speaker
- **Phụ đề:** Không phụ đề / Hardsub (ghi cứng vào video) / Softsub (track `.srt`/`.vtt` rời, bật/tắt được)
- **Logo/Watermark:** file ảnh, vị trí (mặc định trên cùng - bên phải), chiều rộng (px, mặc định 150)
- **Auto-publish:** YouTube Channel / Facebook Page

---

## Technical stack

### Core
- **ASP.NET Core 8/9** — Web API
- **MediatR** — CQRS (commands/queries) trong tầng Application
- **FluentValidation** — validation
- **EF Core + PostgreSQL** — ORM + DB (JSONB cho metadata linh hoạt)

### Background processing & messaging
- **MassTransit + RabbitMQ** — tách Phase 1 / Phase 2 thành 2 consumer riêng, scale worker độc lập với API
- **SignalR** — push tiến độ pipeline real-time về frontend

### Tích hợp CLI tools
- **CliWrap** — wrap `yt-dlp`, `ffmpeg`, `demucs` (async, streaming stdout/stderr, hỗ trợ cancellation, parse progress)

### AI / Speech
- **whisper.net** — STT chạy local (miễn phí, không cần Whisper API)
- **Azure Cognitive Services Speech SDK** — TTS đa ngôn ngữ (vd tiếng Việt: HoaiMy/NamMinh; Anh, Trung, Nhật... mỗi ngôn ngữ có bộ giọng riêng); SSML control tốc độ/cao độ. Danh sách giọng lọc theo `AudioLanguage`. **Dùng free tier 500K ký tự/tháng.**
- **OpenAI SDK — GPT-4.1 Nano** — dịch thuật, gọi qua **Batch API** (giảm 50%) + prompt caching cho system prompt

### Storage & infra
- **Cloudflare R2** (S3-compatible, qua AWS SDK for .NET) — video output. Chọn R2 vì **không phí egress** + free tier 10 GB/tháng
- **Local filesystem (volume mount)** — file intermediate (audio/vocals/sub), không cần đẩy cloud
- ~~Redis~~ — không cần cho bản local 1 instance (chỉ cần khi scale nhiều instance)
- **Serilog** — structured logging (trace lỗi từng stage)
- **Polly** — retry cho external API (OpenAI, Azure timeout...)

### Testing
- **xUnit** + **FluentAssertions** + **NSubstitute** — unit test, mock I/O
- **Coverlet** (threshold gate ≥ 90%) + **ReportGenerator**
- **Testcontainers** (DB/queue thật) + **WireMock.Net** (fake API) + **Verify** (snapshot) — integration. Chi tiết ở mục [Testing & TDD](#testing--tdd).

---

## Cấu trúc thư mục

```
AutoTranslateAI
├── src
│   ├── Api
│   │   ├── Controllers
│   │   └── Hubs                 # SignalR cho progress
│   ├── Application
│   │   ├── Features             # MediatR commands/queries
│   │   ├── Pipeline             # IPipelineStep, PipelineContext, StepResult
│   │   ├── Interfaces           # IDemucsService, ITtsService, IWorkspaceManager...
│   │   ├── DTOs
│   │   └── Validators
│   ├── Domain
│   │   ├── Entities             # DubbingJob, JobStep, Segment, PublishResult
│   │   ├── Enums                # JobStatus, VoiceGender, BgmMode, StepType...
│   │   └── ValueObjects
│   ├── Infrastructure
│   │   ├── AI
│   │   │   ├── SpeechToText     # whisper.net
│   │   │   ├── Translation      # GPT-4.1 Nano (OpenAI Batch API)
│   │   │   └── TextToSpeech     # Azure Speech
│   │   ├── Media
│   │   │   ├── FFmpeg           # extract/mix/render
│   │   │   ├── Demucs           # tách BGM
│   │   │   └── Downloader       # yt-dlp
│   │   ├── Storage              # WorkspaceManager (local) + R2 client (output)
│   │   ├── ExternalApis         # YouTube/Facebook publish
│   │   └── Persistence          # DbContext, migrations, repositories
│   ├── Workers
│   │   ├── Jobs                 # orchestrate pipeline (consumers)
│   │   └── Steps                # implement IPipelineStep
│   └── Shared
├── tests
│   ├── Domain.Tests
│   ├── Application.Tests
│   ├── Infrastructure.Tests
│   └── Integration.Tests        # [Trait] — chạy riêng, không tính vào coverage 90%
├── docker
│   ├── Dockerfile.api
│   └── Dockerfile.worker        # chứa cả Python env + ffmpeg + yt-dlp + demucs
├── docker-compose.yml
├── README.md
└── AutoTranslateAI.sln
```

### Nguyên tắc thiết kế

- **Pipeline contract** (`IPipelineStep`, `PipelineContext`, `StepResult`) đặt ở **Application**, implementation ở **Workers/Steps** — tránh rò rỉ logic nghiệp vụ xuống Workers, dễ test.
- Mỗi external tool có **interface riêng** ở `Application/Interfaces` (`IVideoDownloader`, `IDemucsService`, `ISpeechToTextService`...) và **adapter riêng** ở Infrastructure.
- **`IWorkspaceManager`** quản lý working directory per-job + cleanup file trung gian (audio.wav, vocals.wav, sub.srt, tts...).

---

## Database design

### `DubbingJobs` — entity trung tâm

| Cột | Kiểu | Ghi chú |
|-----|------|---------|
| Id | uuid PK | |
| SourceUrl | text | URL nguồn (null nếu local) |
| LocalFilePath | text | đường dẫn file local |
| SourceLanguage | varchar(10) | "en", "zh"... (null = auto-detect) |
| AudioLanguage | varchar(10) | ngôn ngữ giọng đọc: "vi", "en", "zh-CN"... |
| SubtitleLanguage | varchar(10) | ngôn ngữ phụ đề (độc lập với audio); null nếu không phụ đề |
| EnableDubbing | bool | true = lồng tiếng; false = giữ audio gốc, chỉ thêm sub |
| VoiceGender | int | enum Female/Male |
| BgmMode | int | DemucsAI / Duck / None |
| DuckingDb | int | mặc định -12 |
| SubtitleMode | int | enum None / Hardsub / Softsub |
| AutoPublishYoutube | bool | |
| AutoPublishFacebook | bool | |
| ManualVoiceAssign | bool | gán giọng nhân vật thủ công |
| LogoPath | text | |
| LogoPosition | int | enum vị trí logo |
| LogoWidth | int | px |
| Status | int | enum (xem state machine) |
| CurrentStep | int | step đang chạy |
| ProgressPercent | int | |
| ErrorMessage | text | |
| OutputFilePath | text | |
| WorkspacePath | text | thư mục tạm của job |
| RowVersion (xmin) | — | concurrency token |
| CreatedAt | timestamptz | |
| StartedAt | timestamptz | |
| ReviewReadyAt | timestamptz | lúc Phase 1 xong |
| ConfirmedAt | timestamptz | lúc user confirm |
| CompletedAt | timestamptz | |
| UserId | uuid FK | |

### `JobSteps` — track + resume từng stage

| Cột | Kiểu | Ghi chú |
|-----|------|---------|
| Id | uuid PK | |
| JobId | uuid FK | |
| StepType | int | Download/ExtractAudio/SeparateBgm/Transcribe/Translate/Tts/GenSubtitle/Mix/Render/Publish |
| Status | int | Pending/Running/Completed/Failed/Skipped |
| Phase | int | 1 hoặc 2 |
| StartedAt | timestamptz | |
| CompletedAt | timestamptz | |
| DurationMs | bigint | |
| OutputPath | text | file output của step |
| ErrorMessage | text | |
| RetryCount | int | |
| CreatedAt | timestamptz | |

> Bảng này cho phép **resume từ step bị fail** thay vì chạy lại từ đầu — quan trọng vì mỗi lần fail tốn tiền API + thời gian.

### `Segments` — transcript editable (bảng user tương tác nhiều nhất)

| Cột | Kiểu | Ghi chú |
|-----|------|---------|
| Id | uuid PK | |
| JobId | uuid FK | |
| SegmentIndex | int | |
| StartTime | double | giây |
| EndTime | double | giây |
| OriginalText | text | tiếng gốc video (từ Whisper) |
| AudioTextAi | text | bản dịch AI cho giọng đọc (null nếu audio = ngôn ngữ gốc) |
| AudioTextEdited | text | bản user sửa cho giọng đọc (null nếu chưa sửa) |
| SubtitleTextAi | text | bản dịch AI cho phụ đề (null nếu sub = gốc hoặc = audio) |
| SubtitleTextEdited | text | bản user sửa cho phụ đề (null nếu chưa sửa) |
| IsEdited | bool | |
| SpeakerLabel | varchar(50) | multi-speaker |
| AssignedVoice | varchar(100) | voice ID cho segment |
| TtsAudioPath | text | file TTS của segment |
| TtsDurationMs | bigint | để check sync |
| NeedsTtsRegenerate | bool | true nếu user sửa text audio sau khi TTS đã tạo |
| RowVersion (xmin) | — | concurrency |
| CreatedAt | timestamptz | |
| UpdatedAt | timestamptz | |

> - Mỗi loại text (audio / subtitle) tách bản AI khỏi bản user sửa để giữ gốc AI (cho phép revert).
> - **Text dùng cho TTS** = `COALESCE(AudioTextEdited, AudioTextAi, OriginalText)`.
> - **Text dùng cho phụ đề** = `COALESCE(SubtitleTextEdited, SubtitleTextAi, AudioTextEdited, AudioTextAi, OriginalText)` — tùy combo (xem bảng "Logic dịch" bên dưới), cột nào null sẽ rơi xuống nguồn phù hợp.
> - `NeedsTtsRegenerate` để Phase 2 chỉ tạo lại TTS cho segment có text audio đã đổi, tiết kiệm tiền/thời gian.

### `PublishResults`

| Cột | Kiểu | Ghi chú |
|-----|------|---------|
| Id | uuid PK | |
| JobId | uuid FK | |
| Platform | int | YouTube/Facebook |
| ExternalId | varchar(100) | video ID sau upload |
| Url | text | |
| Status | int | |
| PublishedAt | timestamptz | |
| ErrorMessage | text | |

> **Hangfire note:** nếu dùng Hangfire thay MassTransit, nó tự tạo bảng `hangfire.*` cho queue/retry.

### Job Status state machine

```csharp
enum JobStatus {
    Queued,
    DownloadingMedia,
    ProcessingPhase1,    // extract / separate / transcribe / translate
    AwaitingReview,      // ⏸ chờ user — điểm dừng quan trọng
    ConfirmedQueued,     // user đã confirm, chờ Phase 2
    ProcessingPhase2,    // tts / mix / render
    Publishing,
    Completed,
    Failed,
    Cancelled
}
```

> Nên enforce transition hợp lệ trong Domain entity (vd `DubbingJob.MarkAwaitingReview()`) thay vì set status tùy tiện từ ngoài.

### Enums phụ đề & ngôn ngữ

```csharp
enum SubtitleMode {
    None,        // không phụ đề
    Hardsub,     // ghi cứng vào khung hình (burn-in)
    Softsub      // track .srt/.vtt rời, người xem bật/tắt được
}
```

> - `AudioLanguage` / `SubtitleLanguage` / `SourceLanguage` lưu mã ngôn ngữ chuẩn (BCP-47: "vi", "en", "zh-CN", "ja"...) thay vì enum cứng, để dễ mở rộng ngôn ngữ mới.
> - `AudioLanguage` quyết định bản dịch cho TTS + bộ giọng (lọc giọng Azure theo `AudioLanguage` + `VoiceGender`).
> - `SubtitleLanguage` quyết định ngôn ngữ phụ đề, **độc lập** với audio.

### Logic dịch (1 hay 2 lần — tối ưu chi phí LLM)

Vì ngôn ngữ audio và phụ đề chọn độc lập, bước Translate cần quyết định gọi LLM bao nhiêu lần dựa trên 3 ngôn ngữ (gốc / audio / sub):

| Audio language | Subtitle language | Số lần dịch | Ghi chú |
|---|---|---|---|
| = gốc | = gốc | **0** | Giữ audio gốc, sub = transcript gốc |
| khác gốc | = audio | **1** | Dịch 1 lần, dùng chung TTS + sub |
| khác gốc | = gốc | **1** | Dịch cho audio; sub = transcript gốc |
| = gốc | khác gốc | **1** | Giữ audio gốc; dịch riêng cho sub |
| khác gốc | khác audio & khác gốc | **2** | Dịch 2 lần độc lập |

> Logic này nằm ở bước Translate. Nếu `EnableDubbing = false` thì không cần bản dịch audio, chỉ xét nhánh subtitle.
> Ví dụ học tiếng Anh phổ biến: video gốc Anh → audio = Anh (giữ gốc, **0 dịch cho audio**) + sub = Việt (**1 dịch**) → tổng 1 lần dịch.

### Concurrency (PostgreSQL)

Dùng `xmin` system column làm optimistic concurrency token:

```csharp
modelBuilder.Entity<Segment>().UseXminAsConcurrencyToken();
modelBuilder.Entity<DubbingJob>().UseXminAsConcurrencyToken();
```

Hai chỗ cần concurrency:
- **Segments** — user mở 2 tab, hoặc sửa khi worker đang đọc.
- **DubbingJobs.Status** — worker và user action (confirm/cancel) có thể race (vd user bấm Cancel đúng lúc worker chuyển Phase 2).

Handle `DbUpdateConcurrencyException` gracefully (reload + báo user).

---

## Worker architecture

Dùng **MassTransit + RabbitMQ**: Phase 1 và Phase 2 là 2 message/consumer riêng. Pause = đơn giản không gửi message Phase 2 cho tới khi user confirm.

```
API: tạo job ───────────────► publish StartPhase1Command
                                      │
Worker Phase1Consumer: chạy ◄─────────┘
   Download → ExtractAudio → SeparateBgm → Transcribe → Translate
   xong → set AwaitingReview, KHÔNG publish gì
                                      │
User: sửa segments → bấm Confirm ─────► API publish StartPhase2Command
                                      │
Worker Phase2Consumer: chạy ◄─────────┘
   TTS (chỉ segment NeedsTtsRegenerate) → Mix → Render → Publish
```

- Mỗi step trong consumer update `JobSteps`.
- Nếu fail: retry (MassTransit retry policy) và resume từ step fail nhờ `JobSteps.Status`.
- Worker scale độc lập với API.

---

## API endpoints

| Method | Endpoint | Mô tả |
|--------|----------|-------|
| POST | `/api/jobs` | Tạo job (bắt đầu Phase 1) |
| GET | `/api/jobs/{id}` | Poll status / progress |
| GET | `/api/jobs/{id}/segments` | Lấy transcript để review |
| PUT | `/api/jobs/{id}/segments/{segId}` | Sửa 1 segment |
| PUT | `/api/jobs/{id}/segments/bulk` | Sửa nhiều segment |
| POST | `/api/jobs/{id}/review/chat` | **AI assistant**: gửi tin nhắn, nhận đề xuất sửa (không ghi DB) |
| GET | `/api/jobs/{id}/review/chat` | Lấy lịch sử hội thoại review |
| POST | `/api/jobs/{id}/review/chat/{proposalId}/apply` | Áp dụng 1 đề xuất (qua đường ghi segment có sẵn) |
| POST | `/api/jobs/{id}/confirm` | Confirm → kích hoạt Phase 2 |
| POST | `/api/jobs/{id}/cancel` | Hủy job |
| GET | `/api/jobs/{id}/download` | Tải video output |

**SignalR:** hub `/hubs/jobs` push progress real-time (và tùy chọn stream token phản hồi của AI assistant).

> **AI Review Assistant** — chatbox giúp user sửa transcript/bản dịch bằng hội thoại ("dịch câu 5 tự nhiên hơn", "gộp câu 12-13"). AI **đề xuất**, user duyệt mới ghi. Chỉ hoạt động khi `AwaitingReview`, dùng chung GPT-4.1 Nano, không thêm hạ tầng. Chi tiết ở `REVIEW_ASSISTANT_DESIGN.md`.

---

## Testing & TDD

Dự án phát triển theo **TDD**, mục tiêu **coverage > 90%** trên tầng chứa logic nghiệp vụ (**Domain + Application**). Đặc thù: phần lõi gọi CLI tools (ffmpeg/yt-dlp/demucs) và API ngoài (Azure TTS / GPT-4.1 Nano) — không bao giờ chạy thật trong unit test.

### Nguyên tắc cốt lõi: tách logic khỏi I/O

Mọi external call nằm sau interface (`IVideoDownloader`, `ITtsService`, `ITranslationService`...). Unit test mock toàn bộ I/O. Coverage 90% tính trên Domain + Application — **không** ép lên wrapper CLI/SDK (sẽ dẫn tới test giả tạo).

### Phần test nhiều, dễ đạt coverage cao (mock hết I/O)

Logic dịch 0/1/2 lần · state machine transitions · `COALESCE` chọn text cho TTS/sub · `NeedsTtsRegenerate` · validation · pipeline orchestration (step run/skip/resume) · map giọng theo ngôn ngữ + gender.

### Phần KHÔNG tính vào 90% (loại trừ, test bằng integration riêng)

Wrapper CliWrap gọi ffmpeg/yt-dlp/demucs thật · SDK adapter Azure/OpenAI · EF migrations · `Program.cs`/DI setup. Đánh dấu `[Trait("Category","Integration")]`, chạy nightly/thủ công — không chạy mỗi commit.

### Phân tầng test

```
tests/
├── Domain.Tests/              # thuần logic, không mock — coverage gần 100%
│   ├── DubbingJobStateMachineTests.cs
│   ├── SegmentTextResolutionTests.cs   # COALESCE audio/sub
│   └── TranslationPlannerTests.cs      # logic dịch 0/1/2 lần
├── Application.Tests/         # mock toàn bộ interface I/O
│   ├── PipelineOrchestratorTests.cs    # step skip/run, resume
│   ├── ConfirmJobHandlerTests.cs
│   └── Validators/...
├── Infrastructure.Tests/      # adapter — mock SDK client, KHÔNG gọi thật
└── Integration.Tests/         # [Trait] — chạy riêng, không tính 90%
    └── FfmpegRealTests.cs
```

### Bộ công cụ

| Công cụ | Vai trò |
|---------|---------|
| **xUnit** | Test framework |
| **FluentAssertions** | Assertion đọc dễ |
| **NSubstitute** (hoặc Moq) | Mock interface |
| **Coverlet** | Đo coverage + threshold gate (fail build nếu < 90%) |
| **ReportGenerator** | Báo cáo HTML |
| **Testcontainers** | PostgreSQL/RabbitMQ thật trong Docker tạm (integration) |
| **WireMock.Net** | Fake HTTP cho OpenAI/Azure/R2 — test adapter không tốn tiền |
| **Verify** | Snapshot test output phức tạp (chuỗi lệnh ffmpeg, file SRT) |

### Ví dụ TDD: logic dịch (viết test TRƯỚC)

Bảng "Logic dịch" trong phần Database chính là test spec sẵn có:

```csharp
[Theory]
[InlineData("en", "en", "en", false, 0)] // audio=gốc, sub=gốc → 0
[InlineData("en", "vi", "vi", true,  1)] // audio≠gốc, sub=audio → 1
[InlineData("en", "vi", "en", true,  1)] // audio≠gốc, sub=gốc → 1
[InlineData("en", "en", "vi", false, 1)] // audio=gốc, sub≠gốc → 1
[InlineData("en", "vi", "ja", true,  2)] // tất cả khác → 2
public void DetermineTranslationCount(
    string src, string audio, string sub, bool dubbing, int expected)
{
    var plan = TranslationPlanner.Plan(src, audio, sub, dubbing);
    plan.TranslationCalls.Should().Be(expected);
}
```

> **Lưu ý:** phần **sync độ dài TTS với timestamp** khó TDD thuần vì "đúng" mang tính cảm quan + phụ thuộc output Azure. TDD được phần thuật toán điều chỉnh rate (duration → rate factor là logic thuần), nhưng chất lượng cuối vẫn cần kiểm thử thủ công. Coverage không thay thế được việc nghe thử.

---

## Chi phí sử dụng

> **Cấu hình chốt (tối ưu chi phí):** STT `whisper.net` local · LLM **GPT-4.1 Nano + Batch** · TTS **Azure free tier 500K ký tự/tháng** · Storage **Cloudflare R2**.
> Với mục đích cá nhân/local, tổng chi phí thực tế **gần như $0/tháng** — chỉ phần LLM dịch chắc chắn tốn nhưng cực rẻ (vài đô cho hàng trăm video). Giá tham khảo tháng 6/2026.

### Bảng chi phí theo cấu hình chốt

| Thành phần | Lựa chọn | Giá | Chi phí thực tế |
|------------|----------|-----|-----------------|
| **STT** | `whisper.net` local | Miễn phí | **$0** — chỉ tốn CPU/GPU máy |
| **LLM dịch** | GPT-4.1 Nano + Batch | $0.10/$0.40 per 1M token (Batch −50%) | **~$0.40 / 100 video** |
| **TTS** | Azure Speech (Neural) | Free 500K ký tự/tháng, sau đó $16/1M | **$0** nếu < ~50 video/tháng |
| **Storage** | Cloudflare R2 | Free 10 GB/tháng, sau đó $0.015/GB, **egress $0** | **$0** nếu < 10 GB video |

### Ước tính chi tiết (video 10 phút, transcript ~10K ký tự)

- **STT:** whisper.net chạy local — **miễn phí**.
- **LLM dịch:** ~4K token input + ~3K token output/video. GPT-4.1 Nano qua Batch ≈ **$0.002/video** (sub khác audio → dịch 2 lần ≈ $0.004). 100 video ≈ **$0.20–0.40**.
- **TTS:** ~10K ký tự/video. Free tier 500K ký tự ≈ **~50 video/tháng miễn phí**. Vượt: ~$0.16/video.
- **Storage R2:** video ~100–200 MB. 10 GB free ≈ ~50–100 video. **Egress miễn phí** (điểm mạnh nhất của R2 — user tải video không tốn tiền).

### Vì sao chọn các thành phần này

- **whisper.net local** — khoản tiết kiệm lớn nhất; Whisper API cloud sẽ tốn liên tục ($0.006/phút).
- **GPT-4.1 Nano** — model rẻ nhất đủ tốt cho dịch (task ngôn ngữ, không cần reasoning). **Batch API giảm 50%** (dịch không cần real-time) + **prompt caching giảm tới 90%** cho system prompt lặp lại.
- **Azure TTS free tier** — 500K ký tự/tháng vĩnh viễn, đủ cho dev/cá nhân.
- **Cloudflare R2** — **không phí egress** (Azure/AWS tính egress đắt khi user tải video) + free 10 GB. S3-compatible nên dùng AWS SDK for .NET.

### Hạ tầng (cố định, không theo video)

| Hạng mục | Ghi chú |
|----------|---------|
| **Máy chạy local** | CPU đủ chạy; **GPU** giúp Demucs + whisper.net nhanh hơn nhiều (không bắt buộc) |
| **DB / RabbitMQ** | Chạy trong Docker local — miễn phí |

### Miễn phí / self-host (open source)

`ffmpeg` · `yt-dlp` · **Demucs** (Meta) · **whisper.net / whisper.cpp** · **.NET** · **PostgreSQL** · **RabbitMQ**.

### Auto-publish (Phase 6, tùy chọn)

| Dịch vụ | Ghi chú |
|---------|---------|
| **YouTube Data API** | Miễn phí nhưng có **quota** upload/ngày; vượt phải xin tăng |
| **Facebook Graph API** | Miễn phí; cần app review + permissions |

### Muốn $0 tuyệt đối (offline hoàn toàn)

Thay 2 thành phần cloud còn lại: **LLM → Ollama** (Qwen2.5/Llama 3.x), **TTS → Piper hoặc Coqui/XTTS** local. Đánh đổi: chất lượng dịch + giọng đọc thường kém hơn Nano/Azure, và tốn tài nguyên máy hơn.

### Tối ưu thêm

- Học tiếng Anh điển hình (audio Anh **giữ gốc** + sub Việt) chỉ tốn **1 lần dịch, không TTS** → rẻ nhất.
- **Lifecycle rule trên R2** + cleanup file intermediate sau mỗi job để storage không phình.
- **Cost tracking** (Phase 7) — hữu ích để theo dõi chi tiêu LLM dù rất nhỏ.

---

## Điểm kỹ thuật cần lưu ý

- **Không thể thuần .NET 100%** — Demucs (và tùy chọn Whisper/yt-dlp) là Python/native tools. Kiến trúc thực tế: **.NET orchestrator điều phối các CLI tool**. Cần đóng gói toàn bộ (.NET app + Python env + ffmpeg + yt-dlp + demucs) bằng Docker để deploy ổn định.

- **Đồng bộ thời lượng giọng đọc với video gốc** là phần tốn công nhất. Tiếng Việt thường dài hơn bản gốc → điều chỉnh `rate` qua SSML hoặc `atempo` filter trong ffmpeg để khớp `EndTime - StartTime`.

- **Dịch theo context** tốt hơn dịch rời từng câu — gửi nhiều segment kèm ngữ cảnh cho LLM.

- **Đa ngôn ngữ:** mỗi ngôn ngữ giãn/co độ dài text khác nhau so với bản gốc (vd Đức/Việt dài hơn, Trung/Nhật ngắn hơn) → ảnh hưởng trực tiếp đến việc sync TTS, cần test riêng từng ngôn ngữ. Đảm bảo Whisper/Azure hỗ trợ ngôn ngữ đã chọn trước khi cho phép chọn.

- **Audio ≠ Subtitle language:** giọng đọc và phụ đề chọn độc lập (vd nói Anh, sub Việt). Tối ưu chi phí bằng cách dịch 1 hay 2 lần tùy combo (xem bảng "Logic dịch"). Khi sub khác audio, validate riêng việc sync sub không cần khớp với độ dài TTS — phụ đề bám theo timestamp gốc, không bám theo audio đã lồng tiếng.

- **Softsub vs Hardsub:** softsub linh hoạt hơn (người xem bật/tắt, đổi ngôn ngữ phụ đề) nhưng một số nền tảng (vd TikTok) không hiển thị softsub → với các nền tảng đó nên dùng hardsub. Có thể xuất đồng thời cả file phụ đề rời lẫn bản hardsub nếu cần.

- **Cleanup workspace** sau mỗi job để tránh đầy ổ đĩa (file trung gian khá lớn).

### Lệnh CLI tham khảo

```bash
# Download
yt-dlp -f bestvideo+bestaudio "URL" -o output.mp4

# Tách audio
ffmpeg -i input.mp4 -vn -acodec pcm_s16le audio.wav

# Tách BGM/voice
demucs --two-stems=vocals audio.wav   # -> vocals.wav + no_vocals.wav (BGM)

# Mix giọng + nhạc nền (ducking -12dB)
ffmpeg -i tts_voice.wav -i bgm.wav -filter_complex \
  "[1:a]volume=-12dB[bgm];[0:a][bgm]amix=inputs=2" final_audio.wav

# Render cuối — logo trên-phải (luôn áp dụng)
# (1) Không phụ đề:
ffmpeg -i input.mp4 -i final_audio.wav -i logo.png \
  -filter_complex "[0:v][2:v]overlay=W-w-10:10" \
  -map 0:v -map 1:a output.mp4

# (2) Hardsub — burn phụ đề vào khung hình:
ffmpeg -i input.mp4 -i final_audio.wav -i logo.png \
  -filter_complex "[0:v][2:v]overlay=W-w-10:10,subtitles=sub.srt" \
  -map 0:v -map 1:a output.mp4

# (3) Softsub — nhúng track phụ đề rời (bật/tắt được), không burn:
ffmpeg -i input.mp4 -i final_audio.wav -i logo.png -i sub.srt \
  -filter_complex "[0:v][2:v]overlay=W-w-10:10[v]" \
  -map "[v]" -map 1:a -map 3 -c:s mov_text output.mp4
```

---

## Yêu cầu môi trường

Bản local chỉ cần **Docker + docker-compose** — mọi thứ khác chạy trong container:

- **Docker + docker-compose** (bắt buộc, đủ để chạy toàn bộ)

Bên trong các container (compose tự lo, không cần cài tay trên máy host):
- **.NET 8/9** (image API + Worker)
- **PostgreSQL** + **RabbitMQ** (container riêng)
- **Python** (cho Demucs, optional Whisper) + **ffmpeg** + **yt-dlp** + **demucs** (trong image Worker)

Cấu hình ngoài (API keys / tài khoản) cho cấu hình chốt:
- **OpenAI API key** — cho bước dịch (GPT-4.1 Nano) *(hoặc Ollama local nếu muốn offline)*
- **Azure Speech key** — cho TTS, dùng free tier 500K ký tự/tháng *(hoặc Piper/Coqui local nếu muốn offline)*
- **Cloudflare R2 credentials** — Account ID + Access Key + Secret (S3-compatible) cho video output
- **YouTube / Facebook API** — chỉ khi bật auto-publish (Phase 6, tùy chọn)

> STT (`whisper.net`) chạy local nên **không cần key**. File intermediate lưu qua **volume mount**, chỉ video output đẩy lên R2. Toàn bộ chạy bằng một lệnh `docker-compose up`.