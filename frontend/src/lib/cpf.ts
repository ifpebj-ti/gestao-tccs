export function isValidCpf(value: string): boolean {
  if (!/^([0-9]{11}|[0-9]{3}\.[0-9]{3}\.[0-9]{3}-[0-9]{2})$/.test(value))
    return false;
  const digits = value.replace(/\D/g, '');
  if (/^([0-9])\1{10}$/.test(digits)) return false;
  for (const size of [9, 10]) {
    const sum = [...digits.slice(0, size)].reduce(
      (total, digit, index) => total + Number(digit) * (size + 1 - index),
      0
    );
    const check = (sum * 10) % 11;
    if ((check === 10 ? 0 : check) !== Number(digits[size])) return false;
  }
  return true;
}
