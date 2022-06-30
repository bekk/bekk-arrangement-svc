import { isAuthenticated, redirectToAuth0, useAuth0Redirect } from 'src/auth';
import { Router, Switch, Route, Redirect, RouteProps } from 'react-router';
import React from 'react';
import { Header } from 'src/components/Common/Header/Header';
import { ReactChild } from 'src/types';
import {
  createRoute,
  viewEventRoute,
  eventsRoute,
  cancelParticipantRoute,
  emailKey,
  eventIdKey,
  editEventRoute,
  confirmParticipantRoute,
  previewEventRoute,
  rootRoute,
  previewNewEventRoute,
  viewEventShortnameRoute,
  shortnameKey,
} from 'src/routing';
import { CreateEventContainer } from 'src/components/CreateEvent/CreateEventContainer';
import { EditEventContainer } from 'src/components/EditEvent/EditEventContainer';
import { CancelParticipant } from 'src/components/CancelParticipant/CancelParticipant';
import { createBrowserHistory } from 'history';
import style from './App.module.scss';
import { ConfirmParticipant } from '../ConfirmParticipant/ConfirmParticipant';
import { PreviewEventContainer } from 'src/components/PreviewEvent/PreviewEventContainer';
import { PreviewNewEventContainer } from 'src/components/PreviewEvent/PreviewNewEventContainer';
import { ViewEventsCardsContainer } from 'src/components/ViewEventsCards/ViewEventsCardsContainer';
import { StylingExamples } from 'src/components/Common/StylingExamples/StylingExamples';
import { usePopulateTokensInLocalStorage } from 'src/hooks/saved-tokens';
import {
  ViewEventShortnameRoute,
  ViewEventContainerRegularRoute,
} from 'src/components/ViewEvent/ViewEventRoutes';
import classNames from 'classnames';

const history = createBrowserHistory();

history.listen((location, action) => {
  if (action === 'PUSH') {
    window.scrollTo(0, 0);
  }
});

export const App = () => {
  useAuth0Redirect();
  usePopulateTokensInLocalStorage();
  return (
    <Router history={history}>
      <Header />
      <Switch>
        <PrivateRoute exact path={createRoute}>
          <div className={classNames(style.container, style.lightBackground)}>
            <CreateEventContainer />
          </div>
        </PrivateRoute>
        <PrivateRoute exact path={'/styling'}>
          <div className={classNames(style.container, style.darkBackground)}>
            <StylingExamples />
          </div>
        </PrivateRoute>
        <Route exact path={viewEventRoute(':' + eventIdKey)}>
          <div className={classNames(style.container, style.darkBackground)}>
            <ViewEventContainerRegularRoute />
          </div>
        </Route>
        <PrivateRoute exact path={eventsRoute}>
          <div className={classNames(style.container, style.darkBackground)}>
            <ViewEventsCardsContainer />
          </div>
        </PrivateRoute>
        <PrivateRoute path={editEventRoute(':' + eventIdKey)}>
          <div className={classNames(style.container, style.lightBackground)}>
            <EditEventContainer />
          </div>
        </PrivateRoute>
        <PrivateRoute exact path={previewNewEventRoute}>
          <div className={classNames(style.container, style.lightBackground)}>
            <PreviewNewEventContainer />
          </div>
        </PrivateRoute>
        <PrivateRoute exact path={previewEventRoute(':' + eventIdKey)}>
          <div className={classNames(style.container, style.lightBackground)}>
            <PreviewEventContainer />
          </div>
        </PrivateRoute>
        <Route
          path={cancelParticipantRoute({
            eventId: ':' + eventIdKey,
            email: ':' + emailKey,
          })}
        >
          <div className={classNames(style.container, style.lightBackground)}>
            <CancelParticipant />
          </div>
        </Route>
        <Route
          exact
          path={confirmParticipantRoute({
            eventId: ':' + eventIdKey,
            email: ':' + emailKey,
          })}
        >
          <div className={classNames(style.container, style.darkBackground)}>
            <ConfirmParticipant />
          </div>
        </Route>
        <Redirect exact from={rootRoute} to={eventsRoute} />
        <Route path={viewEventShortnameRoute(':' + shortnameKey)}>
          <div className={classNames(style.container, style.darkBackground)}>
            <ViewEventShortnameRoute />
          </div>
        </Route>
      </Switch>
    </Router>
  );
};
export type ProtectedRouteProps = {
  children: ReactChild;
} & RouteProps;

const PrivateRoute = ({ children, ...routeProps }: ProtectedRouteProps) => {
  if (!isAuthenticated()) {
    redirectToAuth0();
    return null;
  } else {
    return <Route {...routeProps} render={() => children} />;
  }
};
