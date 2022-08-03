import React from 'react';
import { Page } from 'src/components/Page/Page';
import style from './StylingExamples.module.scss';
/**
 * This component is used to display examples of common components, to show how they work.
 * Hopefully this will make it easier to see how changes affect components and know what to use.
 * @returns Page with styling examples
 */
export const StylingExamples = () => {
  return (
    <Page>
      <h1 className={style.title}>Titletext</h1>
      <h2 className={style.header}>Headertext</h2>
      <h3 className={style.subHeader}>Subheadertext</h3>
      <p className={style.regularText}>
        Lorem ipsum dolor sit amet, consectetur adipiscing elit. Quisque et
        tempor lectus. Nullam vehicula, ipsum vel tempor porta, dui mi lacinia
        nibh, vel ullamcorper quam eros sit amet diam. Suspendisse ultricies
        purus nec dignissim posuere. Sed consectetur ex velit, non auctor tellus
        posuere posuere. Sed congue nibh eget erat pretium bibendum. Sed
        sagittis odio ultrices, condimentum ante eleifend, semper odio.
        Pellentesque habitant morbi tristique senectus et netus et malesuada
        fames ac turpis egestas. Suspendisse nunc metus, accumsan ac sapien
        volutpat, interdum feugiat arcu. Orci varius natoque penatibus et magnis
        dis parturient montes, nascetur ridiculus mus. Sed sagittis, risus eu
        vehicula dictum, mi ipsum condimentum ligula, eu suscipit metus turpis
        eu massa. Duis non mauris vestibulum, porta diam eget, pellentesque
        arcu.
      </p>
    </Page>
  );
};
