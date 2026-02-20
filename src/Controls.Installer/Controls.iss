; Скрипт Inno Setup для Controls
; Компилируется в один EXE инсталятор
; Поддержка обновлений с сохранением базы данных

#define MyAppName "Controls"
#define MyAppVersion "1.0.1"
#define MyAppPublisher "Отдел Криминалистики"
#define MyAppURL "https://github.com/your-repo/Controls"
#define MyAppExeName "Controls.exe"
#define MyAppId "{{A1B2C3D4-E5F6-4789-A1B2-C3D4E5F67890}"

[Setup]
; Основная информация
AppId={#MyAppId}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
LicenseFile=License.txt
OutputDir=..\..\bin\Release
OutputBaseFilename=ControlsSetup
SetupIconFile=..\Controls\Controls\Resources\app.ico
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
PrivilegesRequired=admin
UninstallDisplayIcon={app}\{#MyAppExeName}
UninstallDisplayName={#MyAppName}
VersionInfoVersion={#MyAppVersion}
VersionInfoCompany={#MyAppPublisher}
VersionInfoDescription=Система управления заданиями отдела криминалистики
DisableProgramGroupPage=yes

[Languages]
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"

[CustomMessages]
russian.UpdateMode=Обнаружена установленная версия программы
russian.UpdateModeSubCaption=Выберите режим установки
russian.UpdateModeLabel=Обнаружена установленная версия %1.%n%nВыберите действие:
russian.UpdateModeUpdate=Обновить (рекомендуется - сохраняет все данные)
russian.UpdateModeReinstall=Переустановить (удалит и установит заново)
russian.UpdateModeUninstall=Только удалить
russian.BackupDatabase=Создать резервную копию базы данных перед обновлением
russian.ApplicationRunning=Приложение %1 сейчас запущено.%n%nНажмите OK для автоматического закрытия и продолжения установки, или Отмена для выхода.

[Tasks]
Name: "desktopicon"; Description: "Создать ярлык на рабочем столе"; GroupDescription: "Дополнительные значки:"
Name: "autostart"; Description: "Запускать при входе в Windows"; GroupDescription: "Автозапуск:"
Name: "backupdb"; Description: "{cm:BackupDatabase}"; GroupDescription: "Резервное копирование:"; Flags: checkedonce

[Files]
; Все файлы приложения из папки publish (исключаем database при обновлении)
Source: "..\Controls\Controls\bin\Release\net10.0-windows\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Excludes: "database\*"
; Создаем папки для данных (только при новой установке)
Source: "empty.txt"; DestDir: "{app}\database"; Flags: ignoreversion onlyifdoesntexist; AfterInstall: DeleteEmptyFile
Source: "empty.txt"; DestDir: "{app}\logs"; Flags: ignoreversion; AfterInstall: DeleteEmptyFile

[Icons]
; Ярлык в меню Пуск с AppUserModelId для Toast уведомлений
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; AppUserModelID: "Controls.TaskManager"
; Ярлык на рабочем столе
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon; AppUserModelID: "Controls.TaskManager"
; Автозапуск
Name: "{userstartup}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Parameters: "--minimized"; Tasks: autostart; AppUserModelID: "Controls.TaskManager"

[Registry]
; Регистрация приложения для уведомлений
Root: HKCU; Subkey: "Software\Controls.TaskManager"; Flags: uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Controls.TaskManager"; ValueType: string; ValueName: "InstallPath"; ValueData: "{app}"; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Controls.TaskManager"; ValueType: string; ValueName: "Version"; ValueData: "{#MyAppVersion}"; Flags: uninsdeletevalue

[Run]
; Запустить приложение после установки (опционально)
Filename: "{app}\{#MyAppExeName}"; Description: "Запустить {#MyAppName}"; Flags: nowait postinstall skipifsilent

[Code]
var
  UpdateModePage: TInputOptionWizardPage;
  IsUpdate: Boolean;
  InstallMode: Integer; // 0 = новая установка, 1 = обновление, 2 = переустановка, 3 = удаление
  InstalledVersion: String;
  
const
  MODE_UPDATE = 1;
  MODE_REINSTALL = 2;
  MODE_UNINSTALL = 3;

// Удаление временного файла
procedure DeleteEmptyFile();
var
  FilePath: String;
begin
  FilePath := ExpandConstant(CurrentFileName);
  if FileExists(FilePath) then
    DeleteFile(FilePath);
end;

// Проверка и закрытие запущенного приложения
function CloseRunningApplication(): Boolean;
var
  ResultCode: Integer;
  Retries: Integer;
begin
  Result := True;
  Retries := 0;
  
  // Пытаемся закрыть приложение через taskkill
  while (Retries < 3) and (FindWindowByClassName('Window') <> 0) do
  begin
    Exec('taskkill.exe', '/F /IM "{#MyAppExeName}"', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
    Sleep(1000);
    Retries := Retries + 1;
  end;
end;

// Проверка установленной версии через реестр
function GetInstalledVersion(): String;
begin
  Result := '';
  RegQueryStringValue(HKCU, 'Software\Controls.TaskManager', 'Version', Result);
end;

// Получение пути установки из реестра
function GetInstalledPath(): String;
begin
  Result := '';
  RegQueryStringValue(HKCU, 'Software\Controls.TaskManager', 'InstallPath', Result);
end;

// Резервное копирование базы данных
procedure BackupDatabase();
var
  SourcePath: String;
  BackupPath: String;
  BackupDir: String;
  TimeStamp: String;
  FindRec: TFindRec;
begin
  if not IsTaskSelected('backupdb') then
    Exit;
    
  SourcePath := ExpandConstant('{app}\database');
  
  if not DirExists(SourcePath) then
    Exit;
  
  // Создаем метку времени для имени резервной копии
  TimeStamp := GetDateTimeString('yyyymmdd_hhnnss', #0, #0);
  BackupDir := ExpandConstant('{app}\database_backups');
  BackupPath := BackupDir + '\backup_' + TimeStamp;
  
  try
    // Создаем папку для резервных копий
    if not DirExists(BackupDir) then
      ForceDirectories(BackupDir);
    
    // Создаем папку для текущей резервной копии
    if not DirExists(BackupPath) then
      ForceDirectories(BackupPath);
    
    // Копируем все файлы из папки database
    if FindFirst(SourcePath + '\*.*', FindRec) then
    begin
      try
        repeat
          if (FindRec.Attributes and FILE_ATTRIBUTE_DIRECTORY) = 0 then
          begin
            FileCopy(SourcePath + '\' + FindRec.Name, BackupPath + '\' + FindRec.Name, False);
          end;
        until not FindNext(FindRec);
      finally
        FindClose(FindRec);
      end;
      
      MsgBox('✅ Резервная копия базы данных создана в:' + #13#10 + BackupPath, mbInformation, MB_OK);
    end;
  except
    // Игнорируем ошибки резервного копирования
    MsgBox('⚠️ Не удалось создать резервную копию базы данных.' + #13#10 + 'Установка продолжится без резервного копирования.', mbError, MB_OK);
  end;
end;

// Создание страницы выбора режима обновления
procedure CreateUpdateModePage();
begin
  UpdateModePage := CreateInputOptionPage(wpWelcome,
    ExpandConstant('{cm:UpdateMode}'),
    ExpandConstant('{cm:UpdateModeSubCaption}'),
    ExpandConstant('{cm:UpdateModeLabel,' + InstalledVersion + '}'),
    True, False);
  
  UpdateModePage.Add(ExpandConstant('{cm:UpdateModeUpdate}'));
  UpdateModePage.Add(ExpandConstant('{cm:UpdateModeReinstall}'));
  UpdateModePage.Add(ExpandConstant('{cm:UpdateModeUninstall}'));
  
  // По умолчанию выбираем "Обновить"
  UpdateModePage.SelectedValueIndex := 0;
end;

// Инициализация установки
function InitializeSetup(): Boolean;
begin
  Result := True;
  
  InstalledVersion := GetInstalledVersion();
  
  if InstalledVersion <> '' then
  begin
    IsUpdate := True;
    
    // Проверяем, запущено ли приложение
    if CheckForMutexes('{#MyAppId}') then
    begin
      if MsgBox(ExpandConstant('{cm:ApplicationRunning,' + '{#MyAppName}' + '}'), mbConfirmation, MB_OKCANCEL) = IDOK then
      begin
        if not CloseRunningApplication() then
        begin
          MsgBox('Не удалось закрыть приложение. Пожалуйста, закройте его вручную и повторите установку.', mbError, MB_OK);
          Result := False;
          Exit;
        end;
      end
      else
      begin
        Result := False;
        Exit;
      end;
    end;
  end
  else
  begin
    IsUpdate := False;
    InstallMode := 0;
  end;
end;

// Инициализация мастера установки
procedure InitializeWizard();
begin
  if IsUpdate then
  begin
    CreateUpdateModePage();
  end;
end;

// Проверка, нужно ли показывать страницу
function ShouldSkipPage(PageID: Integer): Boolean;
begin
  Result := False;
  
  // Если это обновление и выбрано "Только удалить", пропускаем все страницы кроме деинсталляции
  if IsUpdate and (InstallMode = MODE_UNINSTALL) and (PageID <> wpFinished) then
    Result := True;
end;

// Обработка следующей страницы
function NextButtonClick(CurPageID: Integer): Boolean;
var
  UninstallString: String;
  ResultCode: Integer;
begin
  Result := True;
  
  if IsUpdate and (CurPageID = UpdateModePage.ID) then
  begin
    case UpdateModePage.SelectedValueIndex of
      0: InstallMode := MODE_UPDATE;
      1: InstallMode := MODE_REINSTALL;
      2: InstallMode := MODE_UNINSTALL;
    end;
    
    // Если выбрано "Только удалить"
    if InstallMode = MODE_UNINSTALL then
    begin
      if RegQueryStringValue(HKLM, 'Software\Microsoft\Windows\CurrentVersion\Uninstall\{#MyAppId}_is1', 'UninstallString', UninstallString) or
         RegQueryStringValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Uninstall\{#MyAppId}_is1', 'UninstallString', UninstallString) then
      begin
        if Exec(RemoveQuotes(UninstallString), '/SILENT', '', SW_SHOW, ewWaitUntilTerminated, ResultCode) then
        begin
          MsgBox('Приложение успешно удалено.', mbInformation, MB_OK);
        end;
      end;
      Result := False;
      WizardForm.Close;
      Exit;
    end;
    
    // Если выбрано "Переустановить", удаляем старую версию полностью
    if InstallMode = MODE_REINSTALL then
    begin
      if RegQueryStringValue(HKLM, 'Software\Microsoft\Windows\CurrentVersion\Uninstall\{#MyAppId}_is1', 'UninstallString', UninstallString) or
         RegQueryStringValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Uninstall\{#MyAppId}_is1', 'UninstallString', UninstallString) then
      begin
        Exec(RemoveQuotes(UninstallString), '/SILENT', '', SW_SHOW, ewWaitUntilTerminated, ResultCode);
        Sleep(2000);
      end;
    end;
  end;
  
  // Создаем резервную копию перед установкой файлов
  if (CurPageID = wpReady) and IsUpdate and (InstallMode = MODE_UPDATE) then
  begin
    BackupDatabase();
  end;
end;

// Обработка этапов установки
procedure CurStepChanged(CurStep: TSetupStep);
var
  Message: String;
begin
  // При обновлении - удаляем только программные файлы, оставляем database
  // Это делается автоматически через Excludes в секции [Files]
  if (CurStep = ssInstall) and IsUpdate and (InstallMode = MODE_UPDATE) then
  begin
    // Дополнительная логика при необходимости
  end;
  
  // После успешной установки
  if CurStep = ssPostInstall then
  begin
    if InstallMode = MODE_UPDATE then
    begin
      Message := '✅ Обновление успешно завершено!' + #13#10#13#10 +
                 'База данных сохранена и будет автоматически обновлена при первом запуске приложения.' + #13#10#13#10 +
                 '💡 Entity Framework Core автоматически применит миграции для обновления структуры БД.';
      MsgBox(Message, mbInformation, MB_OK);
    end
    else if InstallMode = MODE_REINSTALL then
    begin
      MsgBox('✅ Переустановка успешно завершена!', mbInformation, MB_OK);
    end;
  end;
end;
