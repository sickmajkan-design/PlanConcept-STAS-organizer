import { Component, type ErrorInfo, type ReactNode } from 'react';

export interface FallbackProps {
  error: unknown;
  /** Drops the caught error and re-renders the children. */
  reset: () => void;
}

interface Props {
  children: ReactNode;
  fallback: (props: FallbackProps) => ReactNode;
  /**
   * Changing this clears a caught error automatically.
   *
   * Without it a boundary is a trap: React keeps rendering the fallback until
   * the boundary's own state is reset, so a crash on one screen would leave
   * the fallback in place while the operator navigates around it. Passing the
   * pathname makes leaving the broken screen the recovery.
   */
  resetKey?: string;
  /** Escape hatch for a test, or for a real reporter later. */
  onError?: (error: unknown, info: ErrorInfo) => void;
}

interface State {
  /** Separate from `error` because throwing `undefined` is legal. */
  hasError: boolean;
  error: unknown;
}

/**
 * Catches a render error below it and shows `fallback` instead.
 *
 * This is the only way React lets you do this — hooks cannot catch a render
 * error, so the class component is required rather than a stylistic leftover.
 * Without one, an exception thrown while rendering unmounts the entire tree
 * and the operator is left looking at a white page with no error, no
 * navigation and no way back other than reloading.
 *
 * It deliberately does not catch everything: an error thrown in an event
 * handler or in an async callback never reaches a boundary. Those go through
 * `toApiError` and the screens' own error states instead.
 */
export class ErrorBoundary extends Component<Props, State> {
  state: State = { hasError: false, error: undefined };

  static getDerivedStateFromError(error: unknown): State {
    return { hasError: true, error };
  }

  componentDidCatch(error: unknown, info: ErrorInfo) {
    this.props.onError?.(error, info);

    // The console is the only sink this app has. React's own message says the
    // error was caught by a boundary but not which component stack produced
    // it, and that stack is the one useful thing here.
    console.error('Unhandled render error', error, info.componentStack);
  }

  componentDidUpdate(previous: Props) {
    if (this.state.hasError && previous.resetKey !== this.props.resetKey) {
      this.reset();
    }
  }

  reset = () => {
    this.setState({ hasError: false, error: undefined });
  };

  render() {
    if (this.state.hasError) {
      return this.props.fallback({ error: this.state.error, reset: this.reset });
    }

    return this.props.children;
  }
}
