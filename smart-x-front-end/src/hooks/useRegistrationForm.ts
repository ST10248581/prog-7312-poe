import { useMemo, useState } from "react";
import { hasErrors, normaliseMacAddress, validateRegistration } from "../utils/validation";
import type { FieldErrors, RegistrationFields as Fields } from "../utils/validation";

/**
 * Form state shared by the register dialog and the registration tab. Errors
 * appear under a field once it has been left (or a submit was attempted), and
 * are rechecked on every keystroke after that, so feedback is immediate but
 * nobody is scolded for a field they have not reached yet. Errors returned by
 * the API (400 validation, 409 duplicate) are shown the same way until that
 * field is edited.
 */
export function useRegistrationForm<T extends Fields>(initial: T) {
  const [form, setForm] = useState<T>(initial);
  const [touched, setTouched] = useState<Partial<Record<keyof T, boolean>>>({});
  const [submitted, setSubmitted] = useState(false);
  const [serverErrors, setServerErrors] = useState<FieldErrors<T>>({});

  const clientErrors = useMemo(() => validateRegistration(form), [form]);
  const isValid = !hasErrors(clientErrors);

  const errorFor = (field: keyof T): string | undefined =>
    serverErrors[field] ?? (touched[field] || submitted ? clientErrors[field] : undefined);

  const update = (field: keyof T, value: string) => {
    setForm((current) => ({ ...current, [field]: value }));
    setServerErrors((current) => {
      if (!(field in current)) return current;
      const next = { ...current };
      delete next[field];
      return next;
    });
  };

  const touch = (field: keyof T) => {
    setTouched((current) => ({ ...current, [field]: true }));
    if (field === "macAddress") {
      setForm((current) => ({ ...current, macAddress: normaliseMacAddress(current.macAddress) }));
    }
  };

  const reset = (next: T) => {
    setForm(next);
    setTouched({});
    setSubmitted(false);
    setServerErrors({});
  };

  return {
    form,
    isValid,
    errorFor,
    update,
    touch,
    reset,
    /** Marks the form as submitted so every remaining error shows. */
    markSubmitted: () => setSubmitted(true),
    setServerErrors: (errors: Record<string, string>) => setServerErrors(errors as FieldErrors<T>),
  };
}

export type RegistrationForm<T extends Fields> = ReturnType<typeof useRegistrationForm<T>>;
