import { POSTER_RULES, fileSize, posterProblem } from './poster-file';

describe('poster file', () => {
  const image = (name: string, size = 1024, type = 'image/png') => new File([new Uint8Array(size)], name, { type });

  it('accepts each allowed image type, whatever the case of its extension', () => {
    expect(posterProblem(image('poster.png'))).toBeNull();
    expect(posterProblem(image('poster.JPG', 1024, 'image/jpeg'))).toBeNull();
    expect(posterProblem(image('poster.webp', 1024, 'image/webp'))).toBeNull();
    expect(posterProblem(image('poster.png', POSTER_RULES.maxBytes))).toBeNull();
  });

  it('refuses other extensions, files without one and files that are not images', () => {
    expect(posterProblem(image('poster.svg', 1024, 'image/svg+xml'))).toContain('JPG, PNG, GIF or WEBP');
    expect(posterProblem(image('poster', 1024))).toContain('JPG, PNG, GIF or WEBP');
    expect(posterProblem(image('notes.png', 1024, 'text/plain'))).toContain('JPG, PNG, GIF or WEBP');
  });

  it('refuses an empty file and one over the size limit', () => {
    expect(posterProblem(image('poster.png', 0))).toBe('This file is empty.');
    expect(posterProblem(image('poster.png', POSTER_RULES.maxBytes + 1))).toBe('The poster can be at most 5 MB; this one is 5.1 MB.');
    expect(posterProblem(image('poster.png', 7 * 1024 * 1024))).toBe('The poster can be at most 5 MB; this one is 7 MB.');
  });

  it('writes sizes in the unit people read them in', () => {
    expect(fileSize(512)).toBe('512 B');
    expect(fileSize(300 * 1024)).toBe('300 KB');
    expect(fileSize(2.41 * 1024 * 1024)).toBe('2.5 MB');
    expect(fileSize(5 * 1024 * 1024)).toBe('5 MB');
  });
});
