import {useState} from "react";
import {Optional} from "src/types";

export function useSessionState<T>(
  initialState: T,
  key: string
): [T, (newState: T) => void];
export function useSessionState<T>(
  key: string,
  initialState?: T,
): [Optional<T>, (newState: T) => void];
export function useSessionState<T>(initialState: T, key: string) {
  const sessionData = window.sessionStorage.getItem(key);
  const toReturn = sessionData === null ? initialState : JSON.parse(sessionData) as T;
  const [sessionState, setSessionState] = useState<T>(toReturn)

  return [
    // The ternary is required here as the useState function above somehow strips fields from the object
    sessionData === null ? toReturn : sessionState,
    ((newState: T) => {
      window.sessionStorage.setItem(key, JSON.stringify(newState));
      setSessionState(newState)
    })
  ]
}