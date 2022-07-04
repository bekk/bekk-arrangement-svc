import React from "react";
import ReactDOM from "react-dom/client";
import { App } from 'src/components/App/App';
import 'src/extension-methods/array';
import 'src/index.css';
import { NotificationHandler } from './components/NotificationHandler/NotificationHandler';
import {getConfig, setConfig} from "src/config";
import {useEffectOnce} from "src/hooks/utils";

const init = async () => {
  const config = await getConfig()
  setConfig(config);
};

const Application = () => {
  useEffectOnce(() => {
    (async () => {
      await init()
    })()
  })
  return(
    <NotificationHandler>
      <App />
    </NotificationHandler>
  )
}

const root = ReactDOM.createRoot(document.getElementById("root")!);
root.render(<Application />);
