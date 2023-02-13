import React, {useState} from 'react';
import style from './Filter.module.scss';
import {FilterIcon} from "../Icons/FilterIcon";
import classNames from "classnames";
import {CheckBox} from "../Checkbox/CheckBox";

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
                    <Type/>
                    <Kontor/>
                </div>}
        </div>
    )
};

const Kontor = () => {
    return (
        <div>
            <h3>Kontor</h3>
            <CheckBox onDarkBackground label="Oslo" isChecked={true} onChange={() => {
            }}/>
            <CheckBox onDarkBackground label="Trondheim" isChecked={true} onChange={() => {
            }}/>
            <CheckBox onDarkBackground label="Alle" isChecked={true} onChange={() => {
            }}/>
        </div>
    )
}

const Type = () => {
    return (
        <div>
            <h3>Type</h3>
            <div className={style.typeContainer}>
                <div>
                    <CheckBox onDarkBackground label="Kommende arrangementer" isChecked={true} onChange={() => {
                    }}/>
                    <CheckBox onDarkBackground label="Tidligere arrangementer" isChecked={true} onChange={() => {
                    }}/>
                    <CheckBox onDarkBackground label="Mine arrangementer" isChecked={true} onChange={() => {
                    }}/>
                </div>
                <div>
                    <CheckBox onDarkBackground label="Åpent arrangement" isChecked={true} onChange={() => {
                    }}/>
                    <CheckBox onDarkBackground label="Lukket arrangement" isChecked={true} onChange={() => {
                    }}/>
                </div>
            </div>
        </div>
    )
}

