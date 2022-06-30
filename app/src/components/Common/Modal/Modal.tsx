import React from 'react';
import style from './Modal.module.scss';
import { useOnClickOutside, useOnEscape } from 'src/hooks/navigation';
import { ReactChild } from 'src/types';

interface IProps {
  closeModal: () => void;
  header: string;
  children?: ReactChild | ReactChild[];
}

export const Modal = ({ closeModal, header, children }: IProps) => {
  useOnEscape(closeModal);
  const modalRef = useOnClickOutside(closeModal);
  return (
    <div className={style.modalContainer}>
      <section className={style.modal} ref={modalRef}>
        <h2 className={style.header}>{header}</h2>
        <div className={style.modalContent}>{children}</div>
      </section>
    </div>
  );
};
