import { useMemo } from "react";
import { useQuery } from "@tanstack/react-query";
import { EntityPicker, type PickerOption } from "@/components/ui/entity-picker";
import { accountingService } from "@/features/accounting/accountingService";

interface AccountPickerProps {
  value?: string | null;
  onChange: (id: string | null, label?: string) => void;
  placeholder?: string;
  disabled?: boolean;
}

export function AccountPicker({
  value,
  onChange,
  placeholder,
  disabled,
}: AccountPickerProps) {
  const { data, isLoading } = useQuery({
    queryKey: ["accounts-picker"],
    queryFn: () => accountingService.accounts.list(undefined, true),
    staleTime: 60_000,
  });

  const options: PickerOption[] = useMemo(
    () =>
      (data ?? []).map((a) => ({
        id: a.id,
        label: `${a.accountNumber} — ${a.name}`,
        sublabel: `${a.type} · ${a.currencyCode}`,
      })),
    [data],
  );

  return (
    <EntityPicker
      value={value ?? ""}
      onChange={(id, opt) => onChange(id, opt?.label)}
      options={options}
      isLoading={isLoading}
      placeholder={placeholder ?? "Seleccionar cuenta contable…"}
      searchPlaceholder="Buscar por número o nombre…"
      disabled={disabled}
    />
  );
}
