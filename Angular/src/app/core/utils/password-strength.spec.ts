import { passwordStrength } from './password-strength';

describe('password strength', () => {
  it('rates a password by length and character variety', () => {
    expect(passwordStrength('')).toBeNull();
    expect(passwordStrength('abc')).toEqual({ percent: 17, level: 'weak', label: 'Weak' });
    expect(passwordStrength('abcdef1')?.level).toBe('medium');
    expect(passwordStrength('Abcdef1')?.level).toBe('strong');
    expect(passwordStrength('Abcdef1!')?.level).toBe('very-strong');
    expect(passwordStrength('Abcdefghij1!')).toEqual({ percent: 100, level: 'very-strong', label: 'Very Strong' });
  });
});
