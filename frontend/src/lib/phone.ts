export function phoneDigits(value: string): string {
  return value.replace(/\D/g, '');
}

export function isValidProfilePhone(value: string): boolean {
  return (
    value.length <= 20 &&
    /^[0-9() +\-]+$/.test(value) &&
    /^[0-9]{10,11}$/.test(phoneDigits(value))
  );
}

export function formatPhone(value: string): string {
  const digits = phoneDigits(value);
  // Não esconda entradas inválidas nem corte números já cadastrados.
  if (digits.length > 11 || !/^[0-9() +\-]*$/.test(value)) return value;
  if (digits.length < 3) return digits;
  const local = digits.slice(2);
  const split = local.length > 8 ? 5 : 4;
  return `(${digits.slice(0, 2)}) ${local.length > split ? `${local.slice(0, split)}-${local.slice(split)}` : local}`;
}
