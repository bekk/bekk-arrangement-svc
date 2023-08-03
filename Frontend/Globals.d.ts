declare module '*.module.css';
declare module '*.scss' {
  const content: { [className: string]: string };
  export = content;
}
