// Entry point. Swift only allows top-level code in main.swift, so app setup lives here.

import AppKit

final class AppDelegate: NSObject, NSApplicationDelegate {

    private var mainController: MainWindowController?

    func applicationDidFinishLaunching(_ notification: Notification) {
        Dw.applyStoredAppearance()
        buildMenu()

        let controller = MainWindowController()
        mainController = controller
        controller.showWindow(nil)
        NSApp.activate(ignoringOtherApps: true)

        // Requesting the tap before the window exists gives the user a prompt with
        // no app on screen to explain it.
        controller.checkPermission()
    }

    func applicationShouldTerminateAfterLastWindowClosed(_ sender: NSApplication) -> Bool {
        return true
    }

    func applicationWillTerminate(_ notification: Notification) {
        InputEngine.shared.stopPlayback()
        InputEngine.shared.stopTap()
    }

    /// Without a menu bar, Cmd-Q and the standard edit shortcuts don't work.
    private func buildMenu() {
        let mainMenu = NSMenu()

        let appMenuItem = NSMenuItem()
        let appMenu = NSMenu()
        appMenu.addItem(withTitle: "About Dolftasker.2",
                        action: #selector(NSApplication.orderFrontStandardAboutPanel(_:)),
                        keyEquivalent: "")
        appMenu.addItem(.separator())
        appMenu.addItem(withTitle: "Hide Dolftasker.2",
                        action: #selector(NSApplication.hide(_:)), keyEquivalent: "h")
        appMenu.addItem(withTitle: "Quit Dolftasker.2",
                        action: #selector(NSApplication.terminate(_:)), keyEquivalent: "q")
        appMenuItem.submenu = appMenu
        mainMenu.addItem(appMenuItem)

        let editMenuItem = NSMenuItem()
        let editMenu = NSMenu(title: "Edit")
        editMenu.addItem(withTitle: "Cut", action: #selector(NSText.cut(_:)), keyEquivalent: "x")
        editMenu.addItem(withTitle: "Copy", action: #selector(NSText.copy(_:)), keyEquivalent: "c")
        editMenu.addItem(withTitle: "Paste", action: #selector(NSText.paste(_:)), keyEquivalent: "v")
        editMenu.addItem(withTitle: "Select All",
                         action: #selector(NSText.selectAll(_:)), keyEquivalent: "a")
        editMenuItem.submenu = editMenu
        mainMenu.addItem(editMenuItem)

        NSApp.mainMenu = mainMenu
    }
}

let app = NSApplication.shared
let delegate = AppDelegate()
app.delegate = delegate
app.setActivationPolicy(.regular)
app.run()
