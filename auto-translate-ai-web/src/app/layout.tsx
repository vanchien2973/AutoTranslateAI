import type { Metadata } from "next";
import { Inter, JetBrains_Mono, Space_Grotesk } from "next/font/google";

import { Header } from "@/components/layout/Header";
import { Sidebar } from "@/components/layout/Sidebar";
import { Providers } from "./providers";
import "./globals.css";

const spaceGrotesk = Space_Grotesk({
  variable: "--font-space-grotesk",
  subsets: ["latin"],
});

const inter = Inter({
  variable: "--font-inter",
  subsets: ["latin"],
});

const jetbrainsMono = JetBrains_Mono({
  variable: "--font-jetbrains-mono",
  subsets: ["latin"],
});

export const metadata: Metadata = {
  title: "AutoTranslateAI",
  description: "Dub and subtitle video with a human in the loop.",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html
      lang="en"
      className={`${spaceGrotesk.variable} ${inter.variable} ${jetbrainsMono.variable} h-full`}
    >
      <body className="h-full">
        <Providers>
          <div className="flex h-full overflow-hidden">
            <Sidebar />
            <div className="flex min-w-0 flex-1 flex-col">
              <Header />
              <main className="flex-1 overflow-y-auto">{children}</main>
            </div>
          </div>
        </Providers>
      </body>
    </html>
  );
}
