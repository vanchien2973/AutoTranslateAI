export type DiffOp = "same" | "added" | "removed";

export interface DiffPart {
  op: DiffOp;
  text: string;
}

function tokenize(text: string) {
  return text.split(/(\s+)/).filter((token) => token !== "");
}

export function diffWords(before: string, after: string): DiffParts {
  const source = tokenize(before);
  const target = tokenize(after);

  const lcs: number[][] = Array.from({ length: source.length + 1 }, () =>
    new Array<number>(target.length + 1).fill(0),
  );

  for (let i = source.length - 1; i >= 0; i--) {
    for (let j = target.length - 1; j >= 0; j--) {
      lcs[i][j] =
        source[i] === target[j] ? lcs[i + 1][j + 1] + 1 : Math.max(lcs[i + 1][j], lcs[i][j + 1]);
    }
  }

  const parts: DiffPart[] = [];
  let i = 0;
  let j = 0;

  while (i < source.length && j < target.length) {
    if (source[i] === target[j]) {
      push(parts, "same", source[i]);
      i++;
      j++;
    } else if (lcs[i + 1][j] >= lcs[i][j + 1]) {
      push(parts, "removed", source[i]);
      i++;
    } else {
      push(parts, "added", target[j]);
      j++;
    }
  }

  while (i < source.length) push(parts, "removed", source[i++]);
  while (j < target.length) push(parts, "added", target[j++]);

  return {
    before: parts.filter((part) => part.op !== "added"),
    after: parts.filter((part) => part.op !== "removed"),
  };
}

export interface DiffParts {
  before: DiffPart[];
  after: DiffPart[];
}

function push(parts: DiffPart[], op: DiffOp, text: string) {
  const last = parts[parts.length - 1];
  if (last?.op === op) last.text += text;
  else parts.push({ op, text });
}
