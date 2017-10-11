REM SET BUILD=Debug
SET BUILD=Release

COPY ..\src\ServiceStack.Interfaces\bin\%BUILD%\net45\ServiceStack.Interfaces.* ..\lib\net45
COPY ..\src\ServiceStack.Interfaces\bin\%BUILD%\netstandard2.0\ServiceStack.Interfaces.* ..\lib\netstandard2.0
COPY ..\src\ServiceStack.Common\bin\%BUILD%\net45\ServiceStack.Common.* ..\lib\net45
COPY ..\src\ServiceStack.Common\bin\%BUILD%\netstandard2.0\ServiceStack.Common.* ..\lib\netstandard2.0
COPY ..\src\ServiceStack.Common\bin\Signed\net45\ServiceStack.Common.* ..\lib\signed
COPY ..\src\ServiceStack.Client\bin\%BUILD%\net45\ServiceStack.Client.* ..\lib\net45
COPY ..\src\ServiceStack.Client\bin\%BUILD%\netstandard2.0\ServiceStack.Client.* ..\lib\netstandard2.0
COPY ..\src\ServiceStack.Client\bin\Signed\net45\ServiceStack.Client.* ..\lib\signed
COPY ..\src\ServiceStack\bin\%BUILD%\net45\ServiceStack.dll ..\lib\net45
COPY ..\src\ServiceStack\bin\%BUILD%\netstandard2.0\ServiceStack.dll ..\lib\netstandard2.0

COPY ..\src\ServiceStack\bin\%BUILD%\net45\ServiceStack.* ..\..\ServiceStack.Text\lib\net45
COPY ..\src\ServiceStack\bin\%BUILD%\netstandard2.0\ServiceStack.* ..\..\ServiceStack.Text\lib\netstandard2.0

COPY ..\lib\net45\ServiceStack.Interfaces.dll ..\..\ServiceStack.Text\lib\net45
COPY ..\lib\netstandard2.0\ServiceStack.Interfaces.dll ..\..\ServiceStack.Text\lib\netstandard2.0
COPY ..\lib\net45\ServiceStack.Common.dll ..\..\ServiceStack.Text\lib\net45
COPY ..\lib\netstandard2.0\ServiceStack.Common.dll ..\..\ServiceStack.Text\lib\netstandard2.0
COPY ..\lib\net45\ServiceStack.Client.dll ..\..\ServiceStack.Text\lib\net45
COPY ..\lib\netstandard2.0\ServiceStack.Client.dll ..\..\ServiceStack.Text\lib\netstandard2.0
COPY ..\lib\netstandard2.0\ServiceStack.Client.dll ..\..\ServiceStack.Text\lib\netstandard2.0

COPY ..\lib\net45\ServiceStack.Interfaces.dll ..\..\ServiceStack.Redis\lib\net45
COPY ..\lib\netstandard2.0\ServiceStack.Interfaces.dll ..\..\ServiceStack.Redis\lib\netstandard2.0
COPY ..\lib\net45\ServiceStack.Common.dll ..\..\ServiceStack.Redis\lib\net45
COPY ..\lib\netstandard2.0\ServiceStack.Common.dll ..\..\ServiceStack.Redis\lib\netstandard2.0
COPY ..\lib\signed\ServiceStack.Common.dll ..\..\ServiceStack.Redis\lib\signed
COPY ..\lib\net45\ServiceStack.Client.dll ..\..\ServiceStack.Redis\lib\net45
COPY ..\lib\netstandard2.0\ServiceStack.Client.dll ..\..\ServiceStack.Redis\lib\netstandard2.0
COPY ..\lib\netstandard2.0\ServiceStack.Client.dll ..\..\ServiceStack.Redis\lib\netstandard2.0

COPY ..\lib\net45\ServiceStack.Interfaces.dll ..\..\ServiceStack.OrmLite\lib\net45
COPY ..\lib\netstandard2.0\ServiceStack.Interfaces.dll ..\..\ServiceStack.OrmLite\lib\netstandard2.0
COPY ..\lib\net45\ServiceStack.Common.dll ..\..\ServiceStack.OrmLite\lib\net45
COPY ..\lib\netstandard2.0\ServiceStack.Common.dll ..\..\ServiceStack.OrmLite\lib\netstandard2.0
COPY ..\lib\signed\ServiceStack.Common.dll ..\..\ServiceStack.OrmLite\lib\signed
COPY ..\lib\net45\ServiceStack.Client.dll ..\..\ServiceStack.OrmLite\lib\net45
COPY ..\lib\netstandard2.0\ServiceStack.Client.dll ..\..\ServiceStack.OrmLite\lib\netstandard2.0
COPY ..\lib\netstandard2.0\ServiceStack.Client.dll ..\..\ServiceStack.OrmLite\lib\netstandard2.0

COPY ..\lib\net45\ServiceStack.Interfaces.dll ..\..\ServiceStack.Aws\lib\net45
COPY ..\lib\netstandard2.0\ServiceStack.Interfaces.dll ..\..\ServiceStack.Aws\lib\netstandard2.0
COPY ..\lib\net45\ServiceStack.Common.dll ..\..\ServiceStack.Aws\lib\net45
COPY ..\lib\netstandard2.0\ServiceStack.Common.dll ..\..\ServiceStack.Aws\lib\netstandard2.0
COPY ..\lib\net45\ServiceStack.Client.dll ..\..\ServiceStack.Aws\lib\net45
COPY ..\lib\netstandard2.0\ServiceStack.Client.dll ..\..\ServiceStack.Aws\lib\netstandard2.0
COPY ..\lib\netstandard2.0\ServiceStack.Client.dll ..\..\ServiceStack.Aws\lib\netstandard2.0
COPY ..\lib\net45\ServiceStack.dll ..\..\ServiceStack.Aws\lib\net45
COPY ..\lib\netstandard2.0\ServiceStack.dll ..\..\ServiceStack.Aws\lib\netstandard2.0

COPY ..\lib\net45\ServiceStack.Interfaces.dll ..\..\ServiceStack.Admin\lib\net45
COPY ..\lib\netstandard2.0\ServiceStack.Interfaces.dll ..\..\ServiceStack.Admin\lib\netstandard2.0
COPY ..\lib\net45\ServiceStack.Common.dll ..\..\ServiceStack.Admin\lib\net45
COPY ..\lib\netstandard2.0\ServiceStack.Common.dll ..\..\ServiceStack.Admin\lib\netstandard2.0
COPY ..\lib\net45\ServiceStack.Client.dll ..\..\ServiceStack.Admin\lib\net45
COPY ..\lib\netstandard2.0\ServiceStack.Client.dll ..\..\ServiceStack.Admin\lib\netstandard2.0
COPY ..\lib\netstandard2.0\ServiceStack.Client.dll ..\..\ServiceStack.Admin\lib\netstandard2.0
COPY ..\lib\net45\ServiceStack.dll ..\..\ServiceStack.Admin\lib\net45
COPY ..\lib\netstandard2.0\ServiceStack.dll ..\..\ServiceStack.Admin\lib\netstandard2.0

COPY ..\lib\net45\ServiceStack.Interfaces.dll ..\..\ServiceStack.Stripe\lib\net45
COPY ..\lib\netstandard2.0\ServiceStack.Interfaces.dll ..\..\ServiceStack.Stripe\lib\netstandard2.0
COPY ..\lib\net45\ServiceStack.Client.dll ..\..\ServiceStack.Stripe\lib\net45
COPY ..\lib\netstandard2.0\ServiceStack.Client.dll ..\..\ServiceStack.Stripe\lib\netstandard2.0
COPY ..\lib\netstandard2.0\ServiceStack.Client.dll ..\..\ServiceStack.Stripe\lib\netstandard2.0
COPY ..\lib\net45\ServiceStack.Client.dll ..\..\ServiceStack.Stripe\lib\net45


