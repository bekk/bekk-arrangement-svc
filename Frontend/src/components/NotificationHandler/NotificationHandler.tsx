import React, { useState, createContext, useContext, useCallback } from 'react';
import { Notification } from '@bekk/storybook';

export class UserNotification extends Error {
  public title: string;
  public userMessage: string;
  public status: number;
  constructor(userMessage: string, status: number = 0, title: string = 'Feil') {
    super();
    this.title = title;
    this.userMessage = userMessage;
    this.status = status;
  }
}

interface IProps {
  children?: JSX.Element;
}

type NotifyUser = (n: UserNotification) => void;

const NotificationContext = createContext<NotifyUser>(() => undefined);

export const NotificationHandler = ({ children }: IProps) => {
  const [notification, setNotification] =
    useState<UserNotification | undefined>();
  const notifyUser = (n: UserNotification) => setNotification(n);
  return (
    <NotificationContext.Provider value={notifyUser}>
      <>
        {children}
        {notification && (
          <Notification
            key={notification.message}
            notification={{
              type: 'ERROR',
              title: notification.title,
              message: notification.userMessage,
            }}
            onClose={() => setNotification(undefined)}
            visible={true}
          />
        )}
      </>
    </NotificationContext.Provider>
  );
};

/**
 * notify kan kalles manuelt av klientkode med UserNotification
 *
 * catchAndNotify brukes ved å wrappe ein async funksjon som
 * kan feile. Dvs. `async () => ...` blir `catchAndNotify(() => ...)`
 */
export const useNotification = () => {
  const notify: NotifyUser = useCallback(useContext(NotificationContext), []);
  function _catchAndNotify<T>(f: () => Promise<void>): () => Promise<void>;
  function _catchAndNotify<T>(
    f: (x: T) => Promise<void>
  ): (x: T) => Promise<void>;
  function _catchAndNotify<T>(
    f: (x?: T) => Promise<void>
  ): (x?: T) => Promise<void> {
    return (x?: T) =>
      f(x).catch((e) => {
        if (e instanceof UserNotification) {
          notify(e);
        } else {
          return Promise.reject(e);
        }
      });
  }
  const catchAndNotify = useCallback(_catchAndNotify, [notify]);
  return { notify, catchAndNotify };
};
