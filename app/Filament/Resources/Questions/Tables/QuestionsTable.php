<?php

namespace App\Filament\Resources\Questions\Tables;

use Filament\Actions\BulkActionGroup;
use Filament\Actions\DeleteBulkAction;
use Filament\Actions\EditAction;
use Filament\Tables\Table;
use Filament\Tables\Columns\TextColumn;
use Filament\Tables\Columns\IconColumn;

class QuestionsTable
{
    public static function configure(Table $table): Table
    {
        return $table->columns([
        TextColumn::make('step_name')->label('Step Name'),
        IconColumn::make('enabled')->label('Enabled')->boolean(),

    ]);
    }
    public static function canCreate(): bool
{
    return false;
}

}
