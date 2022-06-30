import React, {ReactNode, useState} from "react";
import style from "./InfoBox.module.scss";
import {Lightbulb} from "src/components/Common/Lightbulb/Lightbulb";
import {Chevron} from "src/components/Common/Chevron/Chevron";
import classNames from "classnames";

interface IProps {
  title: string;
  children: ReactNode | ReactNode[];
}

export function InfoBox({ title, children }: IProps) {
  const [isBoxOpen, setIsBoxOpen] = useState(false);
  const toggleBoxIsOpen = () => setIsBoxOpen(!isBoxOpen)

  const chevronStyle = classNames(style.chevron,
    {
      [style.chevronOpen]: isBoxOpen
    });

  const childrenStyle = classNames(style.children,
    {[style.childrenOpen]: isBoxOpen})

  return (
    <div className={style.container} onClick={toggleBoxIsOpen}>
      <div className={style.titleContainer}>
        <div className={style.title}>
          <Lightbulb />
          <p className={style.titleText}>{title}</p>
        </div>
        <Chevron className={chevronStyle} />
      </div>
      <div className={childrenStyle}>
        {children}
      </div>
    </div>
  );
}