export interface SearchSuggestion {
  type: 'event' | 'category' | 'location';
  publicId: string;   
  label: string;
  score: number;
}
