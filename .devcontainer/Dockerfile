FROM mcr.microsoft.com/dotnet/sdk:7.0

# Install dotnet 3.1, 5.0 and 6.0 sdks
RUN curl -sLO https://dot.net/v1/dotnet-install.sh \
    && chmod +x ./dotnet-install.sh \
    && ./dotnet-install.sh --channel 3.1 --install-dir /usr/share/dotnet \
    && ./dotnet-install.sh --channel 5.0 --install-dir /usr/share/dotnet \
    && ./dotnet-install.sh --channel 6.0 --install-dir /usr/share/dotnet \
    && rm ./dotnet-install.sh

# Install procps (for ".NET Core Attach")
RUN apt-get update \
    && apt-get install -y procps
