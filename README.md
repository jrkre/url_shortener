# Url Shortener App

I made this app because I needed a simple way to shorten urls consistently and for free, so I made my own shortener. This is made with .NET core 9 and React.

This app is hosted on my own server at an anonymous url (for those that know), but feel free to download and host yourself!

## Features

* Shorten URLs
* Expring URLs
* Custom URLs
* View URL stats
* View all shortened URLs

## Installation

Dependencies:

* .NET Core 9
* Node.js
* Docker (optional)

Setup:

1. Clone the repository:

   ```bash
   git clone github.com/jrkre/url-shortener.git
   cd url-shortener
    ```

2. Install the .NET dependencies:

    ```bash
    dotnet restore
    ```

3. Install the Node.js dependencies:

    ```bash
    cd ClientApp
    npm install
    ```

4. Build w/ docker (optional):

   If you want to run the app in a Docker container, you can build the Docker image:

   ```bash
   docker compose build --no-cache
   ```

5. Run the app:

    If you want to run the app without Docker, you can run it directly:

    ```bash
    dotnet run
    ```

    Or if you want to run it with Docker (recommended for consistency):

    ```bash
    docker compose up
    ```
6. Access the app:
    Open your web browser and go to `http://localhost:5000` (or the port you configured in docker-compose.yml).

### todo

* dockerfile -- DONE !!!
* prettify - i think it looks pretty good ;)
* setup one-liner - i got a two-liner  ¯\\_(ツ)_/¯  take it or leave it--

* add more features:
* admin panel
* accounts ( big maybe )
* better stats - analytics usage graphs for urls
* better error handling
* better url validation
* better custom url validation
* better custom url suggestions
