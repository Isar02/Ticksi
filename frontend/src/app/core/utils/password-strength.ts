export type PasswordStrengthLevel = 'weak' | 'medium' | 'strong' | 'very-strong';

export interface PasswordStrength {
  percent: number;
  level: PasswordStrengthLevel;
  label: string;
}

const CHECKS: readonly ((password: string) => boolean)[] = [
  password => password.length >= 6,
  password => password.length >= 10,
  password => /[a-z]/.test(password),
  password => /[A-Z]/.test(password),
  password => /[0-9]/.test(password),
  password => /[^a-zA-Z0-9]/.test(password)
];

export function passwordStrength(password: string): PasswordStrength | null {
  if (!password) {
    return null;
  }

  const passed = CHECKS.filter(check => check(password)).length;
  const percent = Math.round((passed / CHECKS.length) * 100);

  if (passed <= 2) return { percent, level: 'weak', label: 'Weak' };
  if (passed === 3) return { percent, level: 'medium', label: 'Medium' };
  if (passed === 4) return { percent, level: 'strong', label: 'Strong' };
  return { percent, level: 'very-strong', label: 'Very Strong' };
}
