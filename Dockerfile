FROM fsharp
EXPOSE 8080
COPY . .
RUN mono ./.paket/paket.bootstrapper.exe
RUN mono ./.paket/paket.exe restore
ENTRYPOINT ["fsharpi", "src/app.fsx"]