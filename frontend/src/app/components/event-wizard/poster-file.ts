// Mirrors FileUpload in the API settings, so a file the API would refuse is never sent.
export const POSTER_RULES = {
  extensions: ['.jpg', '.jpeg', '.png', '.gif', '.webp'],
  maxBytes: 5 * 1024 * 1024
} as const;

export const POSTER_ACCEPT = POSTER_RULES.extensions.join(',');

export function posterProblem(file: File): string | null {
  const dot = file.name.lastIndexOf('.');
  const extension = dot === -1 ? '' : file.name.slice(dot).toLowerCase();

  if (!(POSTER_RULES.extensions as readonly string[]).includes(extension) || !file.type.startsWith('image/')) {
    return 'Choose a JPG, PNG, GIF or WEBP image.';
  }
  if (file.size === 0) return 'This file is empty.';
  if (file.size > POSTER_RULES.maxBytes) {
    return `The poster can be at most ${fileSize(POSTER_RULES.maxBytes)}; this one is ${fileSize(file.size)}.`;
  }
  return null;
}

// Megabytes round up, so a file just over the limit never reads as within it.
export function fileSize(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${Math.round(bytes / 1024)} KB`;
  return `${Math.ceil((bytes / 1024 / 1024) * 10) / 10} MB`;
}
