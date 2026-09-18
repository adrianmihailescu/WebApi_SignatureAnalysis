import { HubConnectionBuilder } from "@microsoft/signalr";
import { HUB_URL } from "./config";
export const createDetectionConnection=()=>new HubConnectionBuilder().withUrl(HUB_URL,{accessTokenFactory:()=>localStorage.getItem("token")??""}).withAutomaticReconnect().build();
