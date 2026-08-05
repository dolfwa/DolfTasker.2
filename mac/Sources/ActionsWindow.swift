// The Actions editor: the recorded steps as an editable table.
// FIRST and LAST carry the only signal colour here, so Apply is ink, not blue.

import AppKit

final class ActionsWindowController: NSWindowController,
                                     NSTableViewDataSource, NSTableViewDelegate {

    var onApply: (([MacroEvent]) -> Void)?

    private var rows: [MacroEvent]
    private let repeatCount: Int

    private var table: NSTableView!
    private var summaryLabel: NSTextField!

    private let keyList = KeyNames.sortedList()

    private enum Column: String {
        case index, action, x, y, key, scroll, wait
    }

    init(events: [MacroEvent], repeatCount: Int) {
        self.rows = events
        self.repeatCount = repeatCount

        let window = NSWindow(
            contentRect: NSRect(x: 0, y: 0, width: 760, height: 470),
            styleMask: [.titled, .closable, .resizable],
            backing: .buffered,
            defer: false)
        window.title = "Dolftasker.2 — Actions"
        window.minSize = NSSize(width: 620, height: 320)
        window.center()
        super.init(window: window)
        buildUI()
        updateSummary()
    }

    required init?(coder: NSCoder) { fatalError("not used") }

    // MARK: - UI

    private func buildUI() {
        guard let window = self.window, let content = window.contentView else { return }
        content.wantsLayer = true
        content.layer?.backgroundColor = Dw.paper.cgColor

        let pad: CGFloat = 10
        let toolbarHeight: CGFloat = 26
        let footerHeight: CGFloat = 26

        // Toolbar
        var x = pad
        let toolbarY = content.bounds.height - pad - toolbarHeight
        for (title, selector) in [("Insert", #selector(insertRow)),
                                  ("Delete", #selector(deleteRows)),
                                  ("▲", #selector(moveUp)),
                                  ("▼", #selector(moveDown))] {
            let width: CGFloat = (title == "▲" || title == "▼") ? 30 : 62
            let button = Dw.quietButton(title, target: self, action: selector)
            button.frame = NSRect(x: x, y: toolbarY, width: width, height: toolbarHeight)
            button.autoresizingMask = [.minYMargin]
            content.addSubview(button)
            x += width + 6
        }

        summaryLabel = Dw.label("", font: Dw.mono, color: Dw.muted, align: .right)
        summaryLabel.frame = NSRect(x: x + 8, y: toolbarY,
                                    width: content.bounds.width - pad - (x + 8), height: toolbarHeight)
        summaryLabel.autoresizingMask = [.width, .minYMargin]
        content.addSubview(summaryLabel)

        // Footer
        let footerY = pad
        let apply = Dw.inkButton("Apply", target: self, action: #selector(applyEdits))
        apply.frame = NSRect(x: content.bounds.width - pad - 76, y: footerY, width: 76, height: footerHeight)
        apply.autoresizingMask = [.minXMargin]
        content.addSubview(apply)

        let cancel = Dw.quietButton("Cancel", target: self, action: #selector(cancelEdits))
        cancel.frame = NSRect(x: content.bounds.width - pad - 76 - 8 - 72, y: footerY,
                              width: 72, height: footerHeight)
        cancel.autoresizingMask = [.minXMargin]
        content.addSubview(cancel)

        let hint = Dw.label("Wait is the pause before that step. Cells that don't apply read —.",
                            color: Dw.muted)
        hint.frame = NSRect(x: pad, y: footerY + 4, width: content.bounds.width - 200, height: 18)
        hint.autoresizingMask = [.width]
        content.addSubview(hint)

        // Table
        let tableTop = footerY + footerHeight + 8
        let tableHeight = toolbarY - 8 - tableTop
        let scroll = NSScrollView(frame: NSRect(x: pad, y: tableTop,
                                                width: content.bounds.width - pad * 2,
                                                height: tableHeight))
        scroll.autoresizingMask = [.width, .height]
        scroll.hasVerticalScroller = true
        scroll.borderType = .lineBorder
        scroll.drawsBackground = true

        table = NSTableView()
        table.usesAlternatingRowBackgroundColors = false
        table.rowHeight = 26
        table.gridStyleMask = [.solidHorizontalGridLineMask, .solidVerticalGridLineMask]
        table.gridColor = Dw.lineSoft
        table.backgroundColor = Dw.surface
        table.allowsMultipleSelection = true
        table.dataSource = self
        table.delegate = self
        table.usesAutomaticRowHeights = false

        addColumn(.index, title: "#", width: 96)
        addColumn(.action, title: "ACTION", width: 132)
        addColumn(.x, title: "X", width: 70)
        addColumn(.y, title: "Y", width: 70)
        addColumn(.key, title: "KEY", width: 128)
        addColumn(.scroll, title: "SCROLL", width: 78)
        addColumn(.wait, title: "WAIT MS", width: 92)

        scroll.documentView = table
        content.addSubview(scroll)
    }

    private func addColumn(_ id: Column, title: String, width: CGFloat) {
        let column = NSTableColumn(identifier: NSUserInterfaceItemIdentifier(id.rawValue))
        column.title = title
        column.width = width
        column.minWidth = 50
        table.addTableColumn(column)
    }

    // MARK: - Data source

    func numberOfRows(in tableView: NSTableView) -> Int { return rows.count }

    func tableView(_ tableView: NSTableView,
                   viewFor tableColumn: NSTableColumn?,
                   row: Int) -> NSView? {
        guard let tableColumn = tableColumn,
              let column = Column(rawValue: tableColumn.identifier.rawValue),
              row < rows.count else { return nil }

        let event = rows[row]
        let name = ActionName.of(event)

        switch column {
        case .index:
            // The run boundaries stay findable once the list scrolls.
            var text = "\(row + 1)"
            var color = Dw.muted
            if rows.count == 1 { text += "  ONLY"; color = Dw.signal }
            else if row == 0 { text += "  FIRST"; color = Dw.signal }
            else if row == rows.count - 1 { text += "  LAST"; color = Dw.signal }
            return cellLabel(text, font: Dw.monoBold, color: color)

        case .action:
            let popup = NSPopUpButton(frame: .zero, pullsDown: false)
            popup.font = Dw.ui
            popup.isBordered = false
            popup.addItems(withTitles: ActionName.all)
            popup.selectItem(withTitle: name)
            popup.tag = row
            popup.target = self
            popup.action = #selector(actionChanged(_:))
            return popup

        case .x:
            guard ActionName.isMove(name) else { return cellLabel("—", color: Dw.muted, align: .right) }
            return editableField(String(Int(event.x)), row: row, column: .x)

        case .y:
            guard ActionName.isMove(name) else { return cellLabel("—", color: Dw.muted, align: .right) }
            return editableField(String(Int(event.y)), row: row, column: .y)

        case .key:
            guard ActionName.isKey(name) else { return cellLabel("—", color: Dw.muted) }
            let popup = NSPopUpButton(frame: .zero, pullsDown: false)
            popup.font = Dw.ui
            popup.isBordered = false
            for entry in keyList { popup.addItem(withTitle: entry.name) }
            popup.selectItem(withTitle: KeyNames.name(event.keyCode))
            popup.tag = row
            popup.target = self
            popup.action = #selector(keyChanged(_:))
            return popup

        case .scroll:
            guard ActionName.isScroll(name) else { return cellLabel("—", color: Dw.muted, align: .right) }
            return editableField(String(event.scrollY), row: row, column: .scroll)

        case .wait:
            return editableField(String(event.delay), row: row, column: .wait,
                                 font: Dw.monoBold, color: Dw.ink)
        }
    }

    private func cellLabel(_ text: String,
                           font: NSFont = Dw.mono,
                           color: NSColor = Dw.second,
                           align: NSTextAlignment = .left) -> NSTextField {
        let field = Dw.label(text, font: font, color: color, align: align)
        return field
    }

    private func editableField(_ text: String,
                               row: Int,
                               column: Column,
                               font: NSFont = Dw.mono,
                               color: NSColor = Dw.second) -> NSTextField {
        let field = NSTextField(string: text)
        field.font = font
        field.textColor = color
        field.alignment = .right
        field.isBordered = false
        field.drawsBackground = false
        field.tag = row
        field.identifier = NSUserInterfaceItemIdentifier(column.rawValue)
        field.target = self
        field.action = #selector(fieldChanged(_:))
        return field
    }

    // MARK: - Editing

    @objc private func actionChanged(_ sender: NSPopUpButton) {
        let row = sender.tag
        guard row < rows.count, let title = sender.titleOfSelectedItem else { return }
        var event = rows[row]
        ActionName.apply(title, to: &event)

        // Give newly relevant fields a usable starting value.
        if ActionName.isKey(title) && event.keyCode == 0 { event.keyCode = 0 }
        if ActionName.isScroll(title) && event.scrollY == 0 { event.scrollY = 1 }

        rows[row] = event
        table.reloadData()
        updateSummary()
    }

    @objc private func keyChanged(_ sender: NSPopUpButton) {
        let row = sender.tag
        let index = sender.indexOfSelectedItem
        guard row < rows.count, index >= 0, index < keyList.count else { return }
        rows[row].keyCode = keyList[index].code
        updateSummary()
    }

    @objc private func fieldChanged(_ sender: NSTextField) {
        let row = sender.tag
        guard row < rows.count,
              let raw = sender.identifier?.rawValue,
              let column = Column(rawValue: raw) else { return }

        let value = sender.integerValue
        switch column {
        case .x: rows[row].x = Double(value)
        case .y: rows[row].y = Double(value)
        case .scroll: rows[row].scrollY = value
        case .wait: rows[row].delay = max(0, value)
        default: break
        }
        updateSummary()
    }

    // MARK: - Row operations

    @objc private func insertRow() {
        let at = table.selectedRow >= 0 ? table.selectedRow + 1 : rows.count
        var event = MacroEvent(kind: .mouseButton, delay: 100)
        event.isDown = true
        rows.insert(event, at: min(at, rows.count))
        table.reloadData()
        table.selectRowIndexes(IndexSet(integer: min(at, rows.count - 1)), byExtendingSelection: false)
        updateSummary()
    }

    @objc private func deleteRows() {
        let selected = table.selectedRowIndexes
        guard !selected.isEmpty else { return }
        for index in selected.sorted(by: >) where index < rows.count {
            rows.remove(at: index)
        }
        table.reloadData()
        updateSummary()
    }

    @objc private func moveUp() { move(by: -1) }
    @objc private func moveDown() { move(by: 1) }

    private func move(by offset: Int) {
        let from = table.selectedRow
        let to = from + offset
        guard from >= 0, from < rows.count, to >= 0, to < rows.count else { return }
        rows.swapAt(from, to)
        table.reloadData()
        table.selectRowIndexes(IndexSet(integer: to), byExtendingSelection: false)
        updateSummary()
    }

    // MARK: - Finish

    @objc private func applyEdits() {
        onApply?(rows)
        close()
    }

    @objc private func cancelEdits() {
        close()
    }

    private func updateSummary() {
        let seconds = Double(MacroFile.totalMilliseconds(rows)) / 1000.0
        let repeatText = repeatCount == 0 ? "∞" : String(repeatCount)
        summaryLabel.stringValue = String(format: "%d steps · %.2f s · repeat %@",
                                          rows.count, seconds, repeatText)
    }
}
