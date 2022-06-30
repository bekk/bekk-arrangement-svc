import React, { useState } from 'react';
import { Button } from 'src/components/Common/Button/Button';
import { Modal } from 'src/components/Common/Modal/Modal';
import { ReactChild } from 'src/types';

interface IProps {
  text: string;
  onConfirm: () => void;
  children: ReactChild | ReactChild[];
}

export function ButtonWithConfirmModal({ text, onConfirm, children }: IProps) {
  const [showModal, setShowModal] = useState(false);
  const confirmAndClose = () => {
    onConfirm();
    setShowModal(false);
  };
  return (
    <>
      <Button onClick={() => setShowModal(true)}>{text}</Button>
      {showModal && (
        <Modal header={text} closeModal={() => setShowModal(false)}>
          <>
            {children}
            <Button color={'Primary'} onClick={confirmAndClose}>
              {text}
            </Button>
          </>
        </Modal>
      )}
    </>
  );
}
