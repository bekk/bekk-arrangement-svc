import React, { createContext, useContext, useReducer } from 'react';
import ReactDOM from 'react-dom';
import { Router, Switch, Route, Redirect } from 'react-router-dom';
import { createBrowserHistory } from 'history';
import { createRoute, eventsRoute, viewEventRoute, editRoute } from './routing';
import { ViewEventsContainer } from './components/ViewEvents/ViewEventsContainer';
import 'src/extension-methods/array';
import './index.css';
import { EditEventContainer } from './components/EditEvent/EditEventContainer';
import { StoreProvider } from './store';
import { CreateEvent } from './components/CreateEvent/CreateEventContainer';
import { ViewEventContainer } from './components/ViewEvent/ViewEventContainer';

export const history = createBrowserHistory();

const App = () => {
  return (
    <StoreProvider>
      <Router history={history}>
        <Switch>
          <Route path={createRoute}>
            <CreateEvent />
          </Route>
          <Route path={viewEventRoute} exact={true}>
            <ViewEventContainer />
          </Route>
          <Route path={eventsRoute} exact={true}>
            <ViewEventsContainer />
          </Route>
          <Route exact path={editRoute}>
            <EditEventContainer />
          </Route>
          <Redirect exact={true} from={'/'} to={eventsRoute} />
        </Switch>
      </Router>
    </StoreProvider>
  );
};

ReactDOM.render(<App />, document.getElementById('root'));
