import "./ComingSoon.css";

interface ComingSoonProps {
  title: string;
}

function ComingSoon({ title }: ComingSoonProps) {
  return (
    <div className="coming-soon">
      <div className="coming-soon-icon">🚧</div>
      <h2>Coming Soon</h2>
      <div className="coming-soon-feature">{title}</div>
      <p>
        This feature is currently under development and will be available in a
        future release.
      </p>
    </div>
  );
}

export default ComingSoon;
