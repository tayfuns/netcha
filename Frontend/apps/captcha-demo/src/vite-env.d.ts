/// <reference types="vite/client" />

declare namespace JSX {
  interface IntrinsicElements {
    'altcha-widget': React.DetailedHTMLProps<React.HTMLAttributes<HTMLElement>, HTMLElement> & {
      challengeurl?: string;
      name?: string;
      language?: string;
      hidelogo?: boolean;
      hidefooter?: boolean;
      auto?: string;
    };
  }
}
