import React from 'react';
import { ViewEventContainer } from 'src/components/ViewEvent/ViewEventContainer';
import { useShortname } from 'src/hooks/cache';
import { isBad, isLoading, isNotRequested } from 'src/remote-data';
import { eventIdKey, shortnameKey } from 'src/routing';
import { useParam } from 'src/utils/browser-state';
import style from './ViewEventContainer.module.scss';

export const ViewEventContainerRegularRoute = () => {
  const eventId = useParam(eventIdKey);
  return <ViewEventContainer eventId={eventId} />;
};

export const ViewEventShortnameRoute = () => {
  const shortname = useParam(shortnameKey);

  const eventId = useShortname(shortname);

  if (isNotRequested(eventId) || isLoading(eventId)) {
    return null;
  }

  if (isBad(eventId)) {
    return (
      <div className={style.fourOhFour}>
        <div>
          <span>404</span> Det finnes ingen arrangement med navn "{shortname}"
        </div>
      </div>
    );
  }

  return <ViewEventContainer eventId={eventId.data} />;
};
