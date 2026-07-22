# FsAssay Web Playground: Implementation Plan

Here is the proposed plan to build a stunning, intuitive WebAssembly/Fable-based web playground for FsAssay, hosted directly on GitHub Pages. 

## 1. Core Objectives
*   **Showcase FsAssay**: Provide an interactive environment to test F# anti-pattern rules.
*   **Bridge the Gap for C# Developers**: Acknowledge the steep F# learning curve by providing side-by-side C# vs. F# examples and AI-assisted explanations.
*   **Zero-Install Experience**: Everything runs in the browser, hosted on GitHub Pages.

## 2. UI/UX Design (Material 3)
*   **Aesthetics**: We will use a sleek **Material 3 (M3)** design system with a toggleable Dark/Light mode, using dynamic colors and elevated surfaces.
*   **Layout**:
    *   **Header**: Brand logo, theme toggle, and a dropdown for "Anti-Pattern Examples".
    *   **Left Panel (Editor)**: A deeply integrated **Monaco Editor** with F# syntax highlighting.
    *   **Right Panel (Results & Learnings)**: 
        *   **Diagnostics**: Real-time display of FsAssay warnings and errors.
        *   **Pro Tips**: Explanations of *why* something is an anti-pattern, tailored for C# developers.
        *   **AI Context**: A section that breaks down the F# concept into simpler terms.

## 3. Technical Architecture Options
Running the full F# Compiler Services (FCS) and custom analyzers purely in the browser is technically challenging but possible. We have two main paths:

### Option A: The "True Wasm" Blazor Approach (Bolero)
*   **Stack**: F# + Bolero (Blazor WebAssembly) + MudBlazor (Material UI).
*   **Pros**: Compiles to actual WebAssembly. Easier to run heavy .NET DLLs (like the F# compiler and FsAssay) directly in the browser.
*   **Cons**: Larger initial payload/download time for the user.

### Option B: The "Fable + React" Approach
*   **Stack**: F# + Fable + React + `@mui/material` (Material 3).
*   **Pros**: Compiles F# to highly optimized JavaScript. Extremely fast load times, rich ecosystem of React UI components, very easy to embed Monaco.
*   **Cons**: Running the actual F# Compiler and FsAssay analyzer live in JS is difficult. We might have to simulate the analyzer results based on predefined examples or use a lightweight backend (though GitHub pages is static only).

## 4. Supported Use Cases to Showcase
We will embed interactive templates for the core FsAssay rules:
1.  `FSA-C01`: `Unchecked.defaultof` (The `null` trap for C# devs).
2.  `FSA-C02`: `.Value` on Options (Forcing unwraps).
3.  `FSA-C03/C04`: `Async.RunSynchronously` (Blocking threads).
4.  `FSA-S03`: Pokemon Exception Handling (`try...with _ -> ()`).

## 5. Deployment
*   We will configure a **GitHub Actions Workflow** that triggers on pushes to the `main` branch.
*   It will compile the web app and automatically publish it to the `gh-pages` branch, making it instantly live.
