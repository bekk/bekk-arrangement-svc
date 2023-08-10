import React from 'react';

interface IProps {
  className?: string;
  title?: string;
}

export function ParticipantIcon({ className, title }: IProps) {
  return (
    <svg
      className={className}
      width="16"
      height="20"
      viewBox="0 0 16 20"
      fill="none"
      xmlns="http://www.w3.org/2000/svg">
      <title>{title || 'Legg til'}</title>
      <circle cx="7.5" cy="4.5" r="4" stroke="currentColor" />
      <path
        d="M15.5 16.3158C15.5 17.2117 15.4969 17.8098 15.3943 18.2494C15.3038 18.6373 15.1473 18.8548 14.8226 19.0206C14.4454 19.2132 13.8225 19.3435 12.7597 19.416C11.7109 19.4875 10.3031 19.5 8.4 19.5C6.29717 19.5 4.73776 19.4876 3.57454 19.4158C2.39762 19.3432 1.69275 19.2121 1.25968 19.012C1.05386 18.917 0.922581 18.8118 0.831091 18.6991C0.738952 18.5857 0.667609 18.4405 0.615407 18.2381C0.503724 17.8051 0.5 17.214 0.5 16.3158C0.5 14.4954 1.05693 13.2256 1.95066 12.3463C3.36805 10.9519 5.42236 10.5 8 10.5C10.7518 10.5 12.5522 10.7027 14.0302 12.3265C14.826 13.2007 15.5 14.4691 15.5 16.3158Z"
        stroke="currentColor"
      />
    </svg>
  );
}
