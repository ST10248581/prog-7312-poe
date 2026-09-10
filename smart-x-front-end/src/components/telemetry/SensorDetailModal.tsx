import { useEffect, useRef } from "react";
import type { SensorDetail } from "../../services/apiService";
import SensorDetailPanel from "./SensorDetailPanel";

interface SensorDetailModalProps {
  detail: SensorDetail | null;
  loading: boolean;
  onClose: () => void;
}

/**
 * Details on demand, presented as a modal so the overview underneath keeps its
 * scroll position and context while one node is investigated.
 */
function SensorDetailModal({ detail, loading, onClose }: SensorDetailModalProps) {
  const dialogRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const handleKey = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        onClose();
      }
    };

    document.addEventListener("keydown", handleKey);

    // Stop the dashboard behind the overlay from scrolling with it.
    const previousOverflow = document.body.style.overflow;
    document.body.style.overflow = "hidden";

    dialogRef.current?.focus();

    return () => {
      document.removeEventListener("keydown", handleKey);
      document.body.style.overflow = previousOverflow;
    };
  }, [onClose]);

  return (
    <div
      className="modal-backdrop"
      onClick={onClose}
      role="presentation"
    >
      <div
        ref={dialogRef}
        className="modal-dialog"
        role="dialog"
        aria-modal="true"
        aria-label={detail ? `${detail.profile.name} details` : "Sensor details"}
        tabIndex={-1}
        onClick={(event) => event.stopPropagation()}
      >
        <SensorDetailPanel detail={detail} loading={loading} onClose={onClose} />
      </div>
    </div>
  );
}

export default SensorDetailModal;
