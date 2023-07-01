# Android Unity Client Application for AR Environment Design with Stable Diffusion

This repository contains the Unity Android client for the project **_AR Environment Design with Stable Diffusion_**


## Setup
Please make sure that your Android device supports ARCore functionalities. Import the Unity project in this repository into your Unity Hub. The client app was developed with **Unity Hub 3.4.2** and **Unity Editor version 2021.3.23f1**. The implementation and used libraries may be incompatible with earlier versions, as recent updates to AR Foundation were considered in the implementation.

## Application Scripts

The main implementation scripts can be found in the following directory:  Assets/ExampleAssets/Scripts/


## Scripts
- **AnchorCreator:** Responsible for handling user input and placing generated meshes in the scene.
- **FileReader:** Reads files required for constructing a mesh.
- **ObjectLoader:** Constructs the mesh given the data from the FileReader.
- **PostMethod:** Communicates with the server, sending client data, receiving resource URLs, and requesting files


## Server Deployment
Please refer to the server repository at [this link](https://github.com/Ertugrulmert/Stable-Diffusion-AR-Environment-Generation/tree/submission) 

## Authors
Mert Ertugrul 
