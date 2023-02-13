import React, {useState} from 'react';
import style from './Filter.module.scss';
import {FilterIcon} from "../Icons/FilterIcon";
import classNames from "classnames";
import {CheckBox} from "../Checkbox/CheckBox";

type Kontor =
    | "Oslo"
    | "Trondheim"
    | "Alle"

type Type =
    | "Kommende"
    | "Tidligere"
    | "Mine"
    | "Åpent"
    | "Lukket"

export const Filter = () => {
    const [showFilterOptions, setShowFilterOptions] = useState(false);

    const filterStyles = classNames(style.filter, {[style.filterOpen]: showFilterOptions})

    return (
        <div className={style.container}>
            <button className={style.buttonReset} onClick={() => setShowFilterOptions(!showFilterOptions)}>
                <FilterIcon className={filterStyles}/>
            </button>
            {showFilterOptions &&
                <div className={style.filters}>
                    <h3>Type</h3>
                    <h3>Kontor</h3>
                    <CheckBox onDarkBackground label="Eplemost" isChecked={true} onChange={() => {
                    }}/>
                </div>}
        </div>
    )
};

const Kontor = () => {
    return (
        <div>
            <p>hei</p>
        </div>
    )
}

const Type = () => {
    return (
        <div>
            <p>hei</p>
        </div>
    )
}

