import { useId, useMemo } from "react";
import type { ReactNode } from "react";
import type { SensorCategory } from "../../services/apiService";
import type { RegistrationForm } from "../../hooks/useRegistrationForm";
import { humanise } from "../../utils/format";
import { DEFAULT_ZONES, SENSOR_CATEGORIES } from "../../utils/validation";
import type { RegistrationFields as Fields } from "../../utils/validation";

interface RegistrationFieldsProps<T extends Fields> {
  state: RegistrationForm<T>;
  zones?: string[];
  disabled?: boolean;
}

/** The registration inputs, each with its label, hint and inline error. */
function RegistrationFields<T extends Fields>({ state, zones, disabled }: RegistrationFieldsProps<T>) {
  const idPrefix = useId();
  const { form, errorFor, update, touch } = state;

  // Keep the current value selectable even if it is not in the list (e.g. a
  // zone the API has not reported yet).
  const zoneOptions = useMemo(() => {
    const list = zones && zones.length > 0 ? zones : DEFAULT_ZONES;
    return form.zone && !list.includes(form.zone) ? [...list, form.zone] : list;
  }, [zones, form.zone]);

  const field = (
    name: keyof T & keyof Fields,
    label: string,
    control: (props: {
      id: string;
      "aria-invalid": boolean;
      "aria-describedby": string | undefined;
      className: string;
      disabled?: boolean;
      onBlur: () => void;
    }) => ReactNode,
    hint?: string
  ) => {
    const id = `${idPrefix}-${String(name)}`;
    const error = errorFor(name);
    const describedBy = error ? `${id}-error` : hint ? `${id}-hint` : undefined;

    return (
      <div className={`payload-field${error ? " has-error" : ""}`}>
        <label className="payload-field-label" htmlFor={id}>
          {label}
        </label>
        {control({
          id,
          "aria-invalid": Boolean(error),
          "aria-describedby": describedBy,
          className: "payload-input",
          disabled,
          onBlur: () => touch(name),
        })}
        {error ? (
          <span id={`${id}-error`} className="payload-field-error" role="alert">
            {error}
          </span>
        ) : (
          hint && (
            <span id={`${id}-hint`} className="payload-field-hint">
              {hint}
            </span>
          )
        )}
      </div>
    );
  };

  return (
    <>
      {form.name !== undefined &&
        field("name", "Device Name", (props) => (
          <input
            {...props}
            type="text"
            maxLength={60}
            autoComplete="off"
            value={form.name}
            placeholder="e.g. Cold Store Temp 02"
            onChange={(event) => update("name", event.target.value)}
          />
        ), "3-60 characters.")}

      {field("macAddress", "MAC Address", (props) => (
        <input
          {...props}
          type="text"
          maxLength={17}
          autoComplete="off"
          spellCheck={false}
          value={form.macAddress}
          placeholder="e.g. 5C:A1:2A:2C:3C:6F"
          onChange={(event) => update("macAddress", event.target.value)}
        />
      ), "Six hex pairs separated by : or -. Must be unique on the mesh.")}

      {field("nodeId", "Node ID", (props) => (
        <input
          {...props}
          type="text"
          maxLength={10}
          autoComplete="off"
          spellCheck={false}
          value={form.nodeId}
          placeholder="e.g. ENV-041"
          onChange={(event) => update("nodeId", event.target.value.toUpperCase())}
        />
      ), "Category prefix and number, e.g. ENV-041. Must be unique.")}

      {field("zone", "Zone", (props) => (
        <select {...props} value={form.zone} onChange={(event) => update("zone", event.target.value)}>
          <option value="" disabled>
            Select a zone…
          </option>
          {zoneOptions.map((zone) => (
            <option key={zone} value={zone}>
              {zone}
            </option>
          ))}
        </select>
      ))}

      {field("room", "Room", (props) => (
        <input
          {...props}
          type="text"
          maxLength={40}
          value={form.room}
          placeholder="e.g. Server Room"
          onChange={(event) => update("room", event.target.value)}
        />
      ))}

      {field("category", "Sensor Category", (props) => (
        <select
          {...props}
          value={form.category}
          onChange={(event) => update("category", event.target.value as SensorCategory)}
        >
          {SENSOR_CATEGORIES.map((category) => (
            <option key={category} value={category}>
              {humanise(category)}
            </option>
          ))}
        </select>
      ))}
    </>
  );
}

export default RegistrationFields;
