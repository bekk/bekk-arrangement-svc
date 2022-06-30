import React from 'react';
import { DateInput } from 'src/components/Common/DateInput/DateInput';
import style from './DateTimeInput.module.scss';
import { ValidationResult } from 'src/components/Common/ValidationResult/ValidationResult';
import {
  TimeInstanceEdit,
  parseEditTimeInstance,
} from 'src/types/time-instance';
import { TimeInput } from '../TimeInput/TimeInput';
import { isValid } from 'src/types/validation';
import classNames from 'classnames';

interface IProps {
  labelDate?: string;
  labelTime?: string;
  value: TimeInstanceEdit;
  onChange: (datetime: TimeInstanceEdit) => void;
  className?: string;
}

export const DateTimeInputWithTimezone = ({
  labelDate,
  labelTime,
  value,
  onChange,
  className,
}: IProps) => {
  const timeInstance = parseEditTimeInstance(value);
  const timezoneOffsetUTC = new Date().getTimezoneOffset() / -60;

  return (
    <>
      <div className={classNames(style.flex, className)}>
        <div className={style.dateInput}>
          <DateInput
            label={labelDate}
            value={value.date}
            onChange={(date) => {
              onChange({
                ...value,
                date,
              });
            }}
          />
        </div>

        <div>
          <TimeInput
            value={value.time}
            label={labelTime}
            onChange={(time) => {
              onChange({
                ...value,
                time,
                timezone: timezoneOffsetUTC,
              });
            }}
          />
        </div>
      </div>
      {!isValid(timeInstance) && (
        <div className={style.errorText}>
          <ValidationResult validationResult={timeInstance} onLightBackground />
        </div>
      )}
    </>
  );
};
