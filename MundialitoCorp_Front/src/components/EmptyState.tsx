export function EmptyState({ children }: { children: React.ReactNode }) {
  return (
    <div className="empty">
      <svg viewBox="0 0 24 24" width="30" height="30" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" aria-hidden="true">
        <circle cx="11" cy="11" r="7" />
        <path d="m16.2 16.2 4.3 4.3M8.5 11h5" />
      </svg>
      <p>{children}</p>
    </div>
  );
}
