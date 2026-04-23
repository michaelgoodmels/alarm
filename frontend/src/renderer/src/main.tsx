import React from "react";
import { createRoot } from "react-dom/client";
import { Theme } from "@radix-ui/themes";
import { App } from "./App";

createRoot(document.getElementById("root")!).render(
  <React.StrictMode>
    <Theme appearance="dark" accentColor="red" grayColor="slate" radius="large" scaling="105%">
      <App />
    </Theme>
  </React.StrictMode>
);
