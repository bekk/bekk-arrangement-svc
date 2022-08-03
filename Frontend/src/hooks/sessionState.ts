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
  const [sessionState, setSessionState] =
    useState(sessionData === null ? initialState : JSON.parse(sessionData) as T)

  return [
    sessionState,
    ((newState: T) => {
      window.sessionStorage.setItem(key, JSON.stringify(newState));
      setSessionState(newState)
    })
  ]
}