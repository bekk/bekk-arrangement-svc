open System

module Program =
    let [<EntryPoint>] main _ =
        Environment.SetEnvironmentVariable("DOCKER_HOST", "unix:///tmp/podman.sock")
        Environment.SetEnvironmentVariable("TESTCONTAINERS_RYUK_DISABLED", "true")
        0
