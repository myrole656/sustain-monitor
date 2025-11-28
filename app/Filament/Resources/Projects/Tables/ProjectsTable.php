<?php

namespace App\Filament\Resources\Projects\Tables;

use Filament\Tables\Table;
use Filament\Tables\Columns\TextColumn;
use Filament\Forms\Components\Textarea;
use Filament\Tables\Columns\BadgeColumn;
use Filament\Actions\EditAction;
use Filament\Actions\DeleteAction;
use Illuminate\Support\Facades\Auth;
use Filament\Actions\Action;
use App\Models\Project;
use Filament\Notifications\Notification;
use Illuminate\Database\Eloquent\Builder;

class ProjectsTable
{
    public static function configure(Table $table): Table
{
    return $table
        ->modifyQueryUsing(function (Builder $query) {

             if (Auth::user()->role === 'admin') {
        return; // Do nothing — shows all
    }
                $query->where('user_id', Auth::id());
            })
        ->columns([
            TextColumn::make('user.name')->label('Responder')->sortable()->searchable(),
            TextColumn::make('project_name')->label('Project Name')->sortable()->searchable(),
            TextColumn::make('project_location')->label('Location')->searchable(),
            TextColumn::make('reg_date')->label('Registered At')->date()->sortable(),
            BadgeColumn::make('status')
                ->label('Status')
                ->getStateUsing(function ($record) {
// Get the related process and its status
                return $record->process?->status ?? 'FAIL';
                })
                ->colors([
                'success' => fn($state) => $state === 'PLATINUM',
                'primary' => fn($state) => $state === 'GOLD',
                'warning' => fn($state) => $state === 'SILVER',
                'danger' => fn($state) => $state === 'CERTIFIED',
                'secondary' => fn($state) => $state === 'FAIL',
                ]),
        ])
->actions([

    // ------------------------------------------------------
    // EDIT / RE-ATTEMPT (same EditAction)
    // ------------------------------------------------------
    EditAction::make()
    ->label('Reattempt')
    ->visible(fn ($record) => 
        auth()->user()?->role !== 'admin' && $record->status === 'approved'
    ),
        


    // ------------------------------------------------------
    // USER → REQUEST RE-ATTEMPT
    // ------------------------------------------------------
    Action::make('requestReattempt')
        ->label(function ($record) {
            if ($record->status === 'pending') {
                return 'Pending Approval';
            }
            return 'Request Re-Attempt';
        })
        ->icon('heroicon-o-arrow-path')
        ->color(fn ($record) =>
            $record->status === 'pending' ? 'gray' : 'warning'
        )
        ->disabled(fn ($record) =>
            $record->status === 'pending'
        )
        ->visible(fn ($record) =>
            !auth()->user()->isAdmin() &&
            $record->status !== 'approved' // Hide when approved because "Re-Attempt" appears
        )
        ->requiresConfirmation()
        ->modalHeading('Request Re-Attempt')
        ->modalDescription('Are you sure you want to request re-attempt? Admin will review it.')
        ->modalSubmitActionLabel('Send Request')
        ->action(function ($record) {

            // Update status to pending
            $record->update([
                'status' => 'pending',
            ]);

            // Notify all admins
            Notification::make()
                ->title('Re-Attempt Request')
                ->body(auth()->user()->name . ' requested re-attempt for project: ' . $record->project_name)
                ->icon('heroicon-o-arrow-path')
                ->sendToDatabase(
                    \App\Models\User::where('role', 'admin')->get()
                );
        }),


    // ------------------------------------------------------
    // ADMIN → APPROVE RE-ATTEMPT
    // ------------------------------------------------------
    Action::make('approveReattempt')
        ->label('Approve Re-Attempt')
        ->icon('heroicon-o-check')
        ->color('success')
        ->visible(fn ($record) =>
            auth()->user()->isAdmin() && $record->status === 'pending'
        )
        ->requiresConfirmation()
        ->modalHeading('Approve Re-Attempt')
        ->modalDescription('Approve this request and allow the user to re-attempt.')
        ->modalSubmitActionLabel('Approve')
        ->action(function ($record) {

            // Set status to approved
            $record->update([
                'status' => 'approved',
            ]);

            // Notify user
            Notification::make()
                ->title('Re-Attempt Approved')
                ->body('Your re-attempt request for project "' . $record->project_name . '" has been approved.')
                ->icon('heroicon-o-check')
                ->sendToDatabase($record->user);
        }),


    // ------------------------------------------------------
    // PDF BUTTON (unchanged)
    // ------------------------------------------------------
    Action::make('pdf')
        ->label('PDF')
        ->icon('heroicon-o-arrow-down-tray')
        ->url(fn ($record) => route('pdf.project', $record->id))
        ->openUrlInNewTab(),


    // ------------------------------------------------------
    // COMMENT (admin only)
    // ------------------------------------------------------
    Action::make('comment')
        ->label('Comment')
        ->color('secondary')
        ->icon('heroicon-o-users')
        ->visible(fn () => auth()->user()?->isAdmin() ?? false)
        ->form([
            Textarea::make('comment')
                ->label('Make a comment')
                ->required(),
        ])
        ->requiresConfirmation()
        ->modalHeading('Give your comment')
        ->modalDescription('Comment will be sent to the project owner.')
        ->modalSubmitActionLabel('Submit')
        ->action(function (Action $action, array $data, $record) {

            Notification::make()
                ->title('Admin Comment')
                ->icon('heroicon-o-users')
                ->body($data['comment'])
                ->sendToDatabase($record->user);
        }),


    // ------------------------------------------------------
    // DELETE BUTTON
    // ------------------------------------------------------
    DeleteAction::make(),

]);


}


}
