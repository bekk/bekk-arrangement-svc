import React from 'react';
import ReactDOM from 'react-dom/client';
import { App } from 'src/components/App/App';
import 'src/extension-methods/array';
import 'src/index.css';
import { NotificationHandler } from './components/NotificationHandler/NotificationHandler';
import { getConfig, setConfig } from 'src/config';
import {Notification, NotificationTypes} from "./components/Common/Notification/Notification";

const init = async () => {
  const config = await getConfig();
  setConfig(config);
  const root = ReactDOM.createRoot(
    document.getElementById('root') as HTMLElement
  );
  root.render(
    <React.StrictMode>
      <NotificationHandler>
          {/*<>*/}
          {/*<Notification notification={{*/}
          {/*    type: "INFO",*/}
          {/*    title: "HEI",*/}
          {/*    message: "DEG"*/}
          {/*}}*/}
          {/*              onClose={() => {}}*/}
          {/*visible/>*/}
        <App />
          {/*</>*/}
      </NotificationHandler>
    </React.StrictMode>
  );
};

init();
