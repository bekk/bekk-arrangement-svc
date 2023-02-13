import * as React from 'react';
import style from './CheckBox.module.scss';
import classnames from "classnames";

const KEY_CODE_ENTER = 13;

interface ILabelProps {
    label: string;
    isChecked: boolean;
    onChange: (value: boolean) => void;
    onDarkBackground?: boolean;
    inline?: boolean;
}

interface IWrapperProps {
    children: JSX.Element;
    isChecked: boolean;
    onChange: (value: boolean) => void;
    onDarkBackground?: boolean;
    inline?: boolean;
}

export function CheckBox({
                             isChecked,
                             onChange,
                             onDarkBackground,
                             inline,
                             ...labelOrChildren
                         }: ILabelProps | IWrapperProps) {
    const label =
        'label' in labelOrChildren
            ? labelOrChildren.label
            : labelOrChildren.children;
    const onCheckboxChange = () => onChange(!isChecked);
    const handleKeyDown = (e: React.KeyboardEvent) => {
        if (e.keyCode === KEY_CODE_ENTER) {
            onCheckboxChange();
        }
    };
    const classes = classnames(style.checkbox, {
        [style.checkboxDarkBg]: onDarkBackground || false,
        [style.checkboxInline]: inline || false,
        [style.checkbox_checked]: isChecked,
        unChecked: !isChecked,
    });
    return (
        <div className={classes}>
            <label>
                <input
                    type="checkbox"
                    checked={isChecked}
                    onChange={onCheckboxChange}
                    onKeyDown={handleKeyDown}
                />
                {label}
            </label>
        </div>
    );
}