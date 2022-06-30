import React from 'react';
import ReactDOM from 'react-dom';
import { getConfig, setConfig } from 'src/config';
import { App } from 'src/components/App/App';
import 'src/extension-methods/array';
import 'src/index.css';
import { NotificationHandler } from './components/NotificationHandler/NotificationHandler';

const init = async () => {
  const config = await getConfig();
  setConfig(config);
  ReactDOM.render(
    <NotificationHandler>
      <App />
    </NotificationHandler>,
    document.getElementById('root')
  );
};

init();
