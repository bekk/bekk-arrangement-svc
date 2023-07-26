import React from 'react';
import classNames from 'classnames';
import { Emoji } from '../Emoji/Emoji';
import style from "./Notification.module.scss"

export type NotificationTypes = 'INFO' | 'WARNING' | 'ERROR' | 'LOCK';

export interface Props {
    notification: {
        type: NotificationTypes;
        title: string;
        message?: string;
    };
    isPopUp?: boolean;
    visible?: boolean;
    onClose?: () => void;
}

function getIcon(type: NotificationTypes): any {
    switch (type) {
        case 'INFO':
            return <Emoji color="green" mood={'happy'} />;
        case 'WARNING':
            return <Emoji color="orange" mood={'neutral'} />;
        case 'ERROR':
            return <Emoji color="red" mood={'sad'} />;
        case 'LOCK':
            return <Emoji color="green" mood={'lock'} />;
        default:
            return <Emoji color="blue" mood={'curious'} />;
    }
}

const Cross = () => {
    return (
        <svg
            width="15"
            height="15"
            viewBox="0 0 15 15"
            fill="none"
            xmlns="http://www.w3.org/2000/svg"
        >
            <rect
                x="1.13672"
                y="0.429443"
                width="18.9985"
                height="1"
                transform="rotate(45 1.13672 0.429443)"
                fill="white"
            />
            <rect
                x="0.429688"
                y="13.8634"
                width="18.9985"
                height="1"
                transform="rotate(-45 0.429688 13.8634)"
                fill="white"
            />
        </svg>
    );
};

export const Notification = ({
                                 notification,
                                 visible,
                                 onClose,
                                 isPopUp = true,
                             }: Props) => {
    const { type, title, message } = notification;

    const notificationType =
        notification.type === 'INFO' || notification.type === 'LOCK'
            ? style.INFO
            : notification.type === 'WARNING'
                ? style.WARNING
                : style.ERROR;

    const closeClass = classNames({
        [style.visible]: visible,
        [style.notVisible]: !visible,
        [style.openAnimation]: visible && isPopUp,
        [style.closeAnimation]: !visible && isPopUp,
    });

    const notificationClass = classNames(
        style.notification,
        notificationType,
        closeClass,
        {
            [style.isPopUp]: isPopUp,
        }
    );
    const checkClass = classNames(style.check, {
        [style.paddingLeft]: isPopUp,
    });

    return (
        <div className={notificationClass}>
            <div className={checkClass}>{getIcon(type)}</div>
            <div className={style.content}>
                <span className={style.title}>{title}</span>
                {message && <span className={style.message}>{message}</span>}
            </div>
            {onClose ? (
                <div>
                    <button className={style.closeButton} onClick={onClose}>
                        <Cross />
                    </button>
                </div>
            ) : null}
        </div>
    );
};