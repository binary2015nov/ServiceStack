SET MSBUILD="C:\Program Files (x86)\Microsoft Visual Studio\Preview\Enterprise\MSBuild\15.0\Bin\MSBuild.exe"

%MSBUILD% build.proj /target:NuGetPack /property:Configuration=Release;RELEASE=true;PatchVersion=0;PatchCoreVersion=0
%MSBUILD% build-sn.proj /target:NuGetPack /property:Configuration=Signed;RELEASE=true;PatchVersion=0
%MSBUILD% build-pcl.proj /target:TeamCityBuild;NuGetPack /property:Configuration=Release;RELEASE=true;PatchVersion=0;PatchCoreVersion=0
%MSBUILD% build-deps.proj /target:Build /property:Configuration=Release;RELEASE=true;PackageVersion=4.5.0

REM Debug Task
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\msbuild.exe /t:rebuild /debug C:\src\ServiceStack\tests\RazorRockstars.BuildTask\RazorRockstars.BuildTask.csproj

