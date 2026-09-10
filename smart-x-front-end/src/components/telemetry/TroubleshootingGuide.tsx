import type { SensorDetail } from "../../services/apiService";

interface TroubleshootingGuideProps {
  detail: SensorDetail;
}

interface Step {
  title: string;
  instruction: string;
}

/**
 * The troubleshoot step of the progression. Steps are chosen from what the
 * telemetry actually shows, so the guidance changes with the fault rather than
 * presenting one generic checklist.
 */
function buildSteps(detail: SensorDetail): Step[] {
  const { profile, thresholds, attachments, recentBatches, alerts } = detail;

  const activeAlerts = alerts.filter((alert) => alert.status === "Active");
  const rejected = recentBatches.reduce((sum, batch) => sum + batch.rejectedCount, 0);
  const disabledThresholds = thresholds.filter((threshold) => !threshold.isEnabled);
  const steps: Step[] = [];

  if (profile.status === "Offline") {
    steps.push({
      title: "Confirm the disconnection",
      instruction: `No telemetry since ${new Date(profile.lastSeenUtc).toLocaleString()}. Check whether the node lost power or dropped its mesh uplink.`,
    });
    steps.push({
      title: "Inspect the last known readings",
      instruction:
        "The chart falls back to the window before the node went quiet. A drift or spike just before the cut-off usually points at the cause.",
    });
  }

  if (profile.status === "Warning") {
    steps.push({
      title: "Check the breaching reading type",
      instruction:
        "Switch between the reading tabs above and look for the series crossing its threshold band.",
    });
  }

  if (rejected > 0) {
    steps.push({
      title: "Review rejected payloads",
      instruction: `${rejected} readings were rejected across the last ${recentBatches.length} batches. Compare the payload shape against the registered sensor category.`,
    });
  }

  if (disabledThresholds.length > 0) {
    steps.push({
      title: "Re-enable disabled thresholds",
      instruction: `${disabledThresholds.length} threshold rule(s) are switched off, so breaches on those reading types raise nothing.`,
    });
  }

  if (attachments.length === 0) {
    steps.push({
      title: "Attach the configuration record",
      instruction:
        "No config file, photo or hardware log is on this profile. Attaching one gives the next responder the deployment context.",
    });
  }

  if (activeAlerts.length === 0 && steps.length === 0) {
    steps.push({
      title: "Nothing to action",
      instruction: "This node is reporting inside its thresholds with no active alerts.",
    });
  }

  return steps;
}

function TroubleshootingGuide({ detail }: TroubleshootingGuideProps) {
  const steps = buildSteps(detail);

  return (
    <div className="troubleshooting">
      <h3 className="troubleshooting-title">Guided troubleshooting</h3>
      <ol className="troubleshooting-steps">
        {steps.map((step, index) => (
          <li key={step.title}>
            <span className="troubleshooting-index">{index + 1}</span>
            <div>
              <span className="troubleshooting-step-title">{step.title}</span>
              <p className="troubleshooting-step-text">{step.instruction}</p>
            </div>
          </li>
        ))}
      </ol>
    </div>
  );
}

export default TroubleshootingGuide;
