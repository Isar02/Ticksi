// The API sends UTC times without a zone, which the browser would read as local time.
export function asUtcTime(time: string): string {
  return /(Z|[+-]\d\d:\d\d)$/.test(time) ? time : `${time}Z`;
}
