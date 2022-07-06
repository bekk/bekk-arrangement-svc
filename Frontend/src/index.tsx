import React from "react";
import ReactDOM from "react-dom/client";
import { App } from 'src/components/App/App';
import 'src/extension-methods/array';
import 'src/index.css';
import { NotificationHandler } from './components/NotificationHandler/NotificationHandler';
import {authenticateUser, isAuthenticated} from "src/auth";
import {getConfig, setConfig} from "src/config";

const init = async () => {
  const config = await getConfig()
  setConfig(config)
  if (!isAuthenticated()) {
    authenticateUser();
  } else {
    const root = ReactDOM.createRoot(document.getElementById("root") as HTMLElement);
    root.render (
      <React.StrictMode>
        <NotificationHandler>
          <App />
        </NotificationHandler>
      </React.StrictMode>
    )
  }
}

init()