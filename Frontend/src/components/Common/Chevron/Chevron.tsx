import * as React from "react";

interface IProps {
  className?: string
}

export const Chevron = ({className = ""}: IProps) => {
  return (
    <div className={className}>
      <svg width="15px" height="9px" viewBox="0 0 15 9" version="1.1">
        <g stroke="none" transform="translate(-257.000000, -156.000000)">
          <g
            id="Group"
            transform="translate(80.000000, 144.000000)"
            fill="#1D1D1D"
          >
            <polygon points="184.5 18.3157266 190.439974 12.4372345 191.568651 13.5542295 185.628677 19.4327216 184.513185 20.5627655 183.358137 19.4457705 177.431349 13.5542295 178.560026 12.4372345" />
          </g>
        </g>
      </svg>
    </div>
  );
};