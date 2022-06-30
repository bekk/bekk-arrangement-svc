import {useEffect} from "react";

export const useSetTitle = (title: string) => {
 useEffect(() => {
   document.title = title;
 }, [title])
}