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
            EditAction::make(),
            DeleteAction::make(),
            Action::make('pdf')
                ->label('PDF')
                ->icon('heroicon-o-arrow-down-tray')
                ->url(fn ($record) => route('pdf.project', $record->id))
                ->openUrlInNewTab(),

            Action::make('comment')
                ->label('Comment')
                ->color('secondary')
                ->icon('heroicon-o-users')

                // Only visible to admin
                ->visible(fn (): bool => auth()->user()?->isAdmin() ?? false) 

                // --- FORM ---
                ->form(function ($record) {
                    return [
                        Textarea::make('comment')
                            ->label('Make a comment')
                            ->required(),
                    ];
                })

                ->requiresConfirmation()
                ->modalHeading('Give your comment')
                ->modalDescription('coment coment coment')
                ->modalSubmitActionLabel('submit')

                // --- ACTION ---
                ->action(function (Action $action, array $data, $record) {
                    Notification::make()
                        ->title('Admin Comment')
                        ->icon('heroicon-o-users')
                        ->body($data['comment'])
                        ->sendToDatabase($record->user);
                }),


        ]);
}


}
