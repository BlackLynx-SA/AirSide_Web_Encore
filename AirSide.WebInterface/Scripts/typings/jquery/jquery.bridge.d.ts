//Definition for sortable
interface JQuery {
    sortable(options: any): JQuery;
    disableSelection(): JQuery;
} 

interface JQueryStatic {
    bigBox(options: any): JQueryStatic;
    smallBox(options: any): JQueryStatic;
    SmartMessageBox(options: any, callback: (buttonPress: string, value: string) => any): JQueryStatic;
}

//interface HTMLElement {
//    src: string;
//    pause(options?: any): HTMLElement;
//    play(options?: any): HTMLElement;
//}