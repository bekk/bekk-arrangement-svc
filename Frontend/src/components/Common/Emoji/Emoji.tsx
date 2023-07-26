import React from 'react';
import {
  havKontrast,
  skyfrittKontrast,
  soloppgangKontrast,
  solnedgangKontrast,
} from 'src/style/colors';
import style from './Emoji.module.scss';
interface IProps {
  color: Smiley;
  mood: Mood;
}

export const Emoji = ({ color, mood }: IProps) => {
  return (
    <div className={style.container}>
      {color === 'green' ? (
        mood === 'lock' ? (
          <Lock color={havKontrast} />
        ) : (
          <Icon color={havKontrast} mood={mood} />
        )
      ) : color === 'blue' ? (
        <Icon color={skyfrittKontrast} mood={mood} />
      ) : color === 'orange' ? (
        <Icon color={soloppgangKontrast} mood={mood} />
      ) : (
        <Icon color={solnedgangKontrast} mood={mood} />
      )}
    </div>
  );
};

const Icon = ({ color, mood }: { color: string; mood: Mood }) => {
  return (
    <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 74 74">
      <title>emoji_1</title>
      <g id="Layer_2" data-name="Layer 2">
        <g id="Layer_1-2" data-name="Layer 1">
          {mood === 'happy' ? (
            <Happy color={color} />
          ) : mood === 'neutral' ? (
            <Neutral color={color} />
          ) : mood === 'sad' ? (
            <Sad color={color} />
          ) : (
            <Curious color={color} />
          )}
        </g>
      </g>
    </svg>
  );
};

type Smiley = 'green' | 'blue' | 'orange' | 'red';
type Mood = 'happy' | 'sad' | 'neutral' | 'curious' | 'lock';

const Happy = ({ color }: { color: string }) => (
  <>
    <path
      d="M22.25,24.38a2.14,2.14,0,1,0-2.14-2.13A2.14,2.14,0,0,0,22.25,24.38Z"
      fill={color}
    />
    <path
      d="M0,0V74H74V0ZM22.25,18.85a3.4,3.4,0,1,1-3.4,3.4A3.4,3.4,0,0,1,22.25,18.85ZM37,58.08A18.18,18.18,0,0,1,18.85,39.92h1.26a16.89,16.89,0,0,0,33.78,0h1.26A18.18,18.18,0,0,1,37,58.08ZM51.75,25.65a3.4,3.4,0,1,1,3.4-3.4A3.41,3.41,0,0,1,51.75,25.65Z"
      fill={color}
    />
    <path
      d="M51.75,20.11a2.14,2.14,0,1,0,2.14,2.14A2.14,2.14,0,0,0,51.75,20.11Z"
      fill={color}
    />
    <path
      d="M22.25,25.65a3.4,3.4,0,1,0-3.4-3.4A3.41,3.41,0,0,0,22.25,25.65Zm0-5.54a2.14,2.14,0,1,1-2.14,2.14A2.14,2.14,0,0,1,22.25,20.11Z"
      fill="#fff"
    />
    <path
      d="M51.75,18.85a3.4,3.4,0,1,0,3.4,3.4A3.4,3.4,0,0,0,51.75,18.85Zm0,5.53a2.14,2.14,0,1,1,2.14-2.13A2.14,2.14,0,0,1,51.75,24.38Z"
      fill="#fff"
    />
    <path
      d="M37,56.81A16.91,16.91,0,0,1,20.11,39.92H18.85a18.15,18.15,0,1,0,36.3,0H53.89A16.91,16.91,0,0,1,37,56.81Z"
      fill="#fff"
    />
  </>
);

const Neutral = ({ color }: { color: string }) => (
  <>
    <path
      d="M22.25,24.38a2.14,2.14,0,1,0-2.14-2.13A2.14,2.14,0,0,0,22.25,24.38Z"
      fill={color}
    />
    <path
      d="M51.75,20.11a2.14,2.14,0,1,0,2.14,2.14A2.14,2.14,0,0,0,51.75,20.11Z"
      fill={color}
    />
    <path
      d="M0,0V74H74V0ZM22.25,18.85a3.4,3.4,0,1,1-3.4,3.4A3.4,3.4,0,0,1,22.25,18.85ZM54.52,50.28h-35V49h35ZM51.75,25.65a3.4,3.4,0,1,1,3.4-3.4A3.41,3.41,0,0,1,51.75,25.65Z"
      fill={color}
    />
    <path
      d="M22.25,25.65a3.4,3.4,0,1,0-3.4-3.4A3.41,3.41,0,0,0,22.25,25.65Zm0-5.54a2.14,2.14,0,1,1-2.14,2.14A2.14,2.14,0,0,1,22.25,20.11Z"
      fill="#fff"
    />
    <path
      d="M51.75,18.85a3.4,3.4,0,1,0,3.4,3.4A3.4,3.4,0,0,0,51.75,18.85Zm0,5.53a2.14,2.14,0,1,1,2.14-2.13A2.14,2.14,0,0,1,51.75,24.38Z"
      fill="#fff"
    />
    <rect x="19.48" y="49.02" width="35.04" height="1.27" fill="#fff" />
  </>
);

const Sad = ({ color }: { color: string }) => (
  <>
    <path
      d="M51.75,20.11a2.14,2.14,0,1,0,2.14,2.14A2.14,2.14,0,0,0,51.75,20.11Z"
      fill={color}
    />
    <path
      d="M0,0V74H74V0ZM22.25,18.85a3.4,3.4,0,1,1-3.4,3.4A3.4,3.4,0,0,1,22.25,18.85ZM53.89,57.44a16.89,16.89,0,0,0-33.78,0H18.85a18.15,18.15,0,0,1,36.3,0ZM51.75,25.65a3.4,3.4,0,1,1,3.4-3.4A3.41,3.41,0,0,1,51.75,25.65Z"
      fill={color}
    />
    <path
      d="M22.25,24.38a2.14,2.14,0,1,0-2.14-2.13A2.14,2.14,0,0,0,22.25,24.38Z"
      fill={color}
    />
    <path
      d="M22.25,25.65a3.4,3.4,0,1,0-3.4-3.4A3.41,3.41,0,0,0,22.25,25.65Zm0-5.54a2.14,2.14,0,1,1-2.14,2.14A2.14,2.14,0,0,1,22.25,20.11Z"
      fill="#fff"
    />
    <path
      d="M51.75,18.85a3.4,3.4,0,1,0,3.4,3.4A3.4,3.4,0,0,0,51.75,18.85Zm0,5.53a2.14,2.14,0,1,1,2.14-2.13A2.14,2.14,0,0,1,51.75,24.38Z"
      fill="#fff"
    />
    <path
      d="M37,39.29A18.17,18.17,0,0,0,18.85,57.44h1.26a16.89,16.89,0,0,1,33.78,0h1.26A18.17,18.17,0,0,0,37,39.29Z"
      fill="#fff"
    />
  </>
);

const Curious = ({ color }: { color: string }) => (
  <>
    <path
      d="M22.25,20.11a2.14,2.14,0,1,0,2.13,2.14A2.14,2.14,0,0,0,22.25,20.11Z"
      fill={color}
    />
    <path
      d="M37,44.75a2.14,2.14,0,1,0,2.13,2.13A2.13,2.13,0,0,0,37,44.75Z"
      fill={color}
    />
    <path
      d="M0,0V74H74V0ZM22.25,25.65a3.4,3.4,0,1,1,3.4-3.4A3.4,3.4,0,0,1,22.25,25.65ZM37,50.28a3.4,3.4,0,1,1,3.4-3.4A3.4,3.4,0,0,1,37,50.28ZM51.75,25.65a3.4,3.4,0,1,1,3.4-3.4A3.41,3.41,0,0,1,51.75,25.65Z"
      fill={color}
    />
    <path
      d="M51.75,20.11a2.14,2.14,0,1,0,2.14,2.14A2.14,2.14,0,0,0,51.75,20.11Z"
      fill={color}
    />
    <path
      d="M51.75,18.85a3.4,3.4,0,1,0,3.4,3.4A3.4,3.4,0,0,0,51.75,18.85Zm0,5.53a2.14,2.14,0,1,1,2.14-2.13A2.14,2.14,0,0,1,51.75,24.38Z"
      fill="#fff"
    />
    <path
      d="M22.25,18.85a3.4,3.4,0,1,0,3.4,3.4A3.4,3.4,0,0,0,22.25,18.85Zm0,5.53a2.14,2.14,0,1,1,2.13-2.13A2.14,2.14,0,0,1,22.25,24.38Z"
      fill="#fff"
    />
    <path
      d="M37,43.48a3.4,3.4,0,1,0,3.4,3.4A3.41,3.41,0,0,0,37,43.48ZM37,49a2.14,2.14,0,1,1,2.13-2.14A2.13,2.13,0,0,1,37,49Z"
      fill="#fff"
    />
  </>
);

const Lock = ({ color }: { color: string }) => (
  <>
    <svg
      width="87"
      height="87"
      viewBox="0 0 87 87"
      fill="none"
      xmlns="http://www.w3.org/2000/svg">
      <rect width="87" height="87" fill={color} />
      <g clipPath="url(#clip0)">
        <path
          d="M62.9008 32.6H23.8008V71.7H62.9008V32.6Z"
          stroke="white"
          strokeWidth="1.5"
          strokeMiterlimit="10"
        />
        <path
          d="M54.4996 32.4V26.8C54.4996 20.7 49.4996 15.7 43.2996 15.7C37.0996 15.7 32.0996 20.7 32.0996 26.8V32.4"
          stroke="white"
          strokeWidth="1.5"
          strokeMiterlimit="10"
        />
        <path
          d="M43.3008 54.1V60.7"
          stroke="white"
          strokeWidth="1.5"
          strokeMiterlimit="10"
        />
        <path
          d="M43.3 54.2C45.6748 54.2 47.6 52.2748 47.6 49.9C47.6 47.5252 45.6748 45.6 43.3 45.6C40.9252 45.6 39 47.5252 39 49.9C39 52.2748 40.9252 54.2 43.3 54.2Z"
          stroke="white"
          strokeWidth="1.5"
          strokeMiterlimit="10"
        />
      </g>
    </svg>
  </>
);
